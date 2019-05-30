/**** 
 * k4_coordinator
 * 
 * F. Guiet 
 * Creation           : 08/05/2019
 * Last modification  : 
 * 
 * Version            : 1
 * 
 * History            : 
 *                      
 * Librairies used    :
 * 
 *  - https://www.airspayce.com/mikem/arduino/RadioHead/
 *  version 1.89
 * 
 * Draguino LoRa shield Wiki
 * 
 * - https://wiki.dragino.com/index.php?title=Lora_Shield
 */

//** Sample ***

//*** Test OHM
//*** @;5;0;1;OHM+2000;;09;|

//*** Test PING
//*** @;14;0;0;PING+250;;57;|

//*** Bad frame
//*** @;34;0;0;PING;;91;|
//*** @;32;0;0;PINGO;;E4;|

//*** INIT
//*** @;211;0;3;INIT;;CD;|

//*** FIRE
//*** @;12;0;1;FIRE+250;1+15;10;|
//*** @;19;0;3;FIRE+250;1+13;17;|
//*** @;1;0;3;FIRE+200;1+9;AE;|
//*** @;2;0;1;FIRE+200;2+2+1;03;|
 
#include <RHReliableDatagram.h>
#include <RH_RF95.h>
#include <SPI.h>

// According to LoRa shield (see wiki)
//#define RFM95_CS 10
//#define RFM95_RST 9
//#define RFM95_INT 2
#define RFM95_CS D0
#define RFM95_RST D6
#define RFM95_INT D8

// Change to 868.0Mhz or other frequency, must match RX's freq!
#define RF95_FREQ 868.0

//DEBUG MODE
// 0 - Non debug mode
// 1 - debug mode
#define DEBUG 0

// Singleton instance of the radio driver
RH_RF95 driver(RFM95_CS, RFM95_INT);

#define COORDINATOR_ADDRESS 0
const String UNKNOWN_FRAME_ID = "0";
const String UNKNOWN_RECEIVER_ADDRESS = "0";
const int WAIT_FOR_FRAME_TIMEOUT = 200; //in ms
const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO";
const String PING_ORDER = "PING";
const char START_FRAME_DELIMITER = '@';
const char END_FRAME_DELIMITER = '|';
const int MIN_FRAME_LENGHT = 17; 
const String ACK_KO_BAD_FRAME_RECEIVED = "KO_S1";
const String ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED = "KO_S2";
const String ACK_KO_TIMEOUT_WAITING_FOR_ACK = "KO_S3";
const String ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER = "KO_S4";
const String ACK_OK_PONG = "OK_S1";
const String ACK_OK_FRAME_RECEIVED = "OK_R1";

// Class to manage message delivery and receipt, using the driver declared above
RHReliableDatagram manager(driver, COORDINATOR_ADDRESS);

bool timeOutSet = false;

void setup() {
   
  Serial.begin(115200);
  while (!Serial) ; // Wait for serial port to be available
  
  if (!manager.init()) {
    printDebug("LoRa (manager) init failed...");
    while(1);
  }

  //
  // Defaults after init are 434.0MHz, 13dBm, Bw = 125 kHz, Cr = 4/5, Sf = 128chips/symbol, CRC on
  //
  
  //5..8 
  driver.setCodingRate4(5);  

  //6..12
  driver.setSpreadingFactor(7);

  //125KHz
  driver.setSignalBandwidth(125000);  

  //Power to max!
 driver.setTxPower(23, false);

  if (!driver.setFrequency(RF95_FREQ)) {
    printDebug("setFrenquency failed");    
    while (1);
  }

  //Time out for ACK reception
  manager.setTimeout(200);

  //Send message retries if no ack has been recept before timeout occured
  manager.setRetries(2);

  printDebug("Coordinator Ready !");
}

bool listenForkQuatreFrame(String &frame) {
  
  //If nothing availbale on Serial just return...
  while(Serial.available() == 0) {
    delay(10);        
  }
                
  char firstCharReceived = Serial.read();
  if (firstCharReceived=='\r' || firstCharReceived=='\n') {
      printDebug("Carriage Return received");                     
      return false;
  }

  //String frame;
  frame.concat(firstCharReceived);
  
  unsigned long entry = millis();
  bool isFrameTimeout = true;
  
  //wait 200 ms to receive frame before timeout
  while (millis() - entry < WAIT_FOR_FRAME_TIMEOUT) {
    
    if (Serial.available()) {
      char received = Serial.read();
      frame.concat(received); 

      if (received == END_FRAME_DELIMITER) {
        isFrameTimeout = false;
        break;
      }
    }    
  }

  if (isFrameTimeout) {
    printDebug("Timeout !! ");
    return false;
  }
  
  return true;  
}

bool frameSanityCheck(String frame) {

  printDebug("Nb of ; : " + String(countCharInString(frame,';')));

  //At least 7 ;  
  if (countCharInString(frame,';') != 7) return false;
  
  //Check frame lenght
  if (frame.length() < MIN_FRAME_LENGHT) return false;
  
  //Check frame start delimiter
  if (frame.charAt(0) != START_FRAME_DELIMITER) return false;
  
  //Check frame end delimiter  
  if (frame.charAt(frame.length() -1) != END_FRAME_DELIMITER) return false;

  //At least CheckSum + END_FRAME_DELIMITER + 1 char
  if (frame.length() <= 5) return false;
  
  //Check checksum
  if (isCheckSumValid(frame))
    return true;
  else
    return false;  
}

int countCharInString(String message, char toFind) {
   int i, count;
   for (i=0, count=0; message[i]; i++)
     count += (message[i] == toFind);

   return count;
}

String getCheckSumValue(String frame) {
  return getFrameChunk(frame, ';', 6);
}

bool isCheckSumValid(String frame) {

  String frameCheckSum = getCheckSumValue(frame);
  
  printDebug("frameCheckSum : " + frameCheckSum);

  //Sanity check
  if (frameCheckSum == "") return false;
  if (frameCheckSum.length() != 2) return false;  

  int lenghtToTake =  frame.length() - 4; 
  String trimmedFrame = frame.substring(0, lenghtToTake);

  printDebug("Computing debug of : " + trimmedFrame);
  
  if (ComputeCheckSum(trimmedFrame) == frameCheckSum) {
    printDebug("Check sum is ok for me");
    return true;
  }

    printDebug("Check sum is not ok for me");
  return false;
}

String ComputeCheckSum(String frame) {
  
  int lenghtToTake = frame.length();
  
  byte buffer[lenghtToTake + 1]; //lenght + end 
  
  frame.getBytes(buffer,lenghtToTake + 1); 
  
  int checkSum = 0;
  for(int i=0;i <= lenghtToTake - 1; i++) {
    checkSum += buffer[i];
    //printDebug(String(buffer[i]));
  }

  printDebug("CheckSum : " + String(checkSum));
  
  checkSum &= 0xff;

  String checkSumStr = String(checkSum, HEX);
  checkSumStr.toUpperCase();
  
  if (checkSumStr.length() == 1) {
    checkSumStr = "0" + checkSumStr;
  }

  printDebug("CheckSum Str : *" + checkSumStr + "*");

  return checkSumStr;
}

void loop() {

  String frame="";
  if (listenForkQuatreFrame(frame)) {

    printDebug("Frame received : *" + frame + "*");
    
    if (!frameSanityCheck(frame)) {    
      printDebug("syntax of received frame KO : " + frame);    
      sendACK(createFrame(UNKNOWN_FRAME_ID, String(COORDINATOR_ADDRESS), UNKNOWN_RECEIVER_ADDRESS, ACK_KO, ACK_KO_BAD_FRAME_RECEIVED));    
    } 
    else {
      sendMessage(frame);
    }      
  }  
}

void sendMessage(String frame) {
  
  String receiverAddress = getReceiverAddressValue(frame);
  String frameId = getFrameIdValue(frame);  

  //Message sent to me ?
  if (receiverAddress == COORDINATOR_ADDRESS) {
    if (getFrameMessageValue(frame) == PING_ORDER) {
      printDebug("Message for me and it is PING...sending PONG :)");
      sendACK(createFrame(frameId, getSenderAddressValue(frame), receiverAddress, ACK_OK, ACK_OK_PONG));         
    }
    else {      
      printDebug("Message for me and but unknown message...");
      sendACK(createFrame(frameId, getSenderAddressValue(frame), receiverAddress, ACK_KO, ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED));
    }
  }
  else {
    printDebug("Message not for me sending message via LoRa");    

    if (!timeOutSet) {
      timeOutSet = true;
      int timeOut = getFrameAckTimeOutValue(frame).toInt();   
      printDebug("Setting timeout to : " + String(timeOut));      
      manager.setTimeout(timeOut);      
    }    

    //Converttion
    uint8_t pdata[frame.length()+1];   
    memcpy (pdata, frame.c_str(), frame.length());
    pdata[frame.length()]='\0';

    //Serial.print("Frame to send : ");
    //Serial.println((char*)pdata);

    unsigned long entry = millis();
    
    //Send a message message and wait for ACK (wait time = timeout * retries time)
    if (manager.sendtoWait(pdata, sizeof(pdata), receiverAddress.toInt())) {
      
      int rssi = driver.lastRssi();
      int snr = driver.lastSNR();

      unsigned long timeOut = millis() - entry;
      
      //Message sent successfully and ACK received here    
      printDebug("Message sent successfully in " + String(timeOut) + "ms");
      sendACK(createFrame(frameId, receiverAddress, String(COORDINATOR_ADDRESS), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr)));
    }
    else {

      unsigned long timeOut = millis() - entry;
            
      //Message faild to be sent...
      printDebug("Timeout (" + String(timeOut) + " ms) waiting for LoRa ACK");  
      sendACK(createFrame(frameId, String(COORDINATOR_ADDRESS), receiverAddress, ACK_KO, ACK_KO_TIMEOUT_WAITING_FOR_ACK));     
    }
  }
}

String getFrameAckTimeOutValue(String frame) {

  String message = getFrameChunk(frame, ';', 4);

  return getFrameChunk(message, '+', 1);
}

String getSenderAddressValue(String frame) {
  return getFrameChunk(frame, ';', 2);
}

String getFrameMessageValue(String frame) {

  String message = getFrameChunk(frame, ';', 4);

  return getFrameChunk(message, '+', 0);
}

String getReceiverAddressValue(String frame) {
  return getFrameChunk(frame, ';', 3);
}

String getFrameIdValue(String frame) {
  return getFrameChunk(frame, ';', 1);
}


String createFrame(String frameId, String senderAddress, String receiverAddress, String message, String message_complement) {
  
  String frame = String(START_FRAME_DELIMITER) + ";" + frameId + ";" + senderAddress + ";" + receiverAddress + ";" + message + ";" + message_complement;  
  frame.concat(";");
  String checkSum = ComputeCheckSum(frame);
  frame.concat(checkSum);
  frame.concat(";");
  frame.concat(String(END_FRAME_DELIMITER));

  printDebug("Frame created : " + frame);  

  return frame;
}

void sendACK(String ackFrame) {
  Serial.print(ackFrame);  
}

String getFrameChunk(String data, char separator, int index) 
{
  int maxIndex = data.length()-1;
  int j=0;
  String chunkVal = "";
  
  for(int i=0;i<=maxIndex && j<=index;i++) {    
    if (data[i]==separator) 
    {
      j++;
      
      if (j>index) 
      {
        chunkVal.trim();
        return chunkVal;
      }
      
      chunkVal=""; 
    }    
    else {
       chunkVal.concat(data[i]);
    }
  }  
  
  return chunkVal;
}

void printDebug(String debugMessage) {
 if (DEBUG) {
    Serial.println(debugMessage);
  }  
}
