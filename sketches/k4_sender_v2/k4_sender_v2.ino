#include <SPI.h>
#include <LoRa.h>

//DEBUG MODE
#define DEBUG 0

const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO";
const char START_FRAME_DELIMITER = '@';
const char END_FRAME_DELIMITER = '|';
const int MIN_FRAME_LENGHT = 17; 
//** Sample ***

//*** Test PING
//*** @;33;0;0;PING;;96;|

//*** Bad frame
//*** @;34;0;0;PING;;91;|
//*** @;32;0;0;PINGO;;E4;|

//*** INIT
//*** @;211;0;3;INIT;;CD;|

//*** FIRE
//*** @;251;0;3;FIRE;3+1+2+3;0D;|

const int WAIT_FOR_FRAME_TIMEOUT = 200; //in ms
const int WAIT_FOR_ACK_TIMEOUT = 500; // in ms
const long FREQ = 868E6;
const int SF = 9;
const long BW = 125E3;
const String MODULE_ADDRESS = "0";
const String UNKNOWN_RECEIVER_ADDRESS = "-1";
const String UNKNOWN_FRAME_ID = "-1";
const String ACK_KO_BAD_FRAME_RECEIVED = "KO_S1";
const String ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED = "KO_S2";
const String ACK_KO_TIMEOUT_WAITING_FOR_ACK = "KO_S3";
const String ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER = "KO_S4";
const String ACK_OK_PONG = "OK_S1";

void setup() {
  
  //Set Serial baudrate
  Serial.begin(9600);
  while (!Serial);

  //Init LoRa
  if (!LoRa.begin(FREQ)) {
    Serial.println("Starting LoRa failed!");
    while (1);
  }
  
  LoRa.setSpreadingFactor(SF);
  LoRa.setSignalBandwidth(BW);

  printDebug("Ready...");
}

void loop() {
  
  //If nothing availbale on Serial just return...
  while(Serial.available() == 0);  //stays on this line until something's in the buffer
  
  String frame = "";
  //String ackFrame = "";
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

  if (isFrameTimeout) 
    printDebug("Timeout !! ");
  
  printDebug("Frame received : *" + frame + "*");

  if (!frameSanityCheck(frame)) {    
    printDebug("syntax of received frame KO : " + frame);    
    sendACK(createFrame(UNKNOWN_FRAME_ID, MODULE_ADDRESS, UNKNOWN_RECEIVER_ADDRESS, ACK_KO, ACK_KO_BAD_FRAME_RECEIVED));    
  }
  else {
    printDebug("Frame syntax OK here");    

    //Send Lora
    sendMessage(frame);    
  }

  printDebug("Purging serial buffer...");
  //Clear serial buffer
  purgeSerialBuffer();
}

void sendMessage(String frame) {
  String receiverAddress = getReceiverAddressValue(frame);

  //Message sent to me ?
  if (receiverAddress == MODULE_ADDRESS) {
    if (getFrameMessageValue(frame) == "PING") {
      printDebug("Message for me and it is PING...sending PONG :)");
      sendACK(createFrame(getFrameIdValue(frame), getSenderAddressValue(frame), getReceiverAddressValue(frame), ACK_OK, ACK_OK_PONG));         
    }
    else {      
      printDebug("Message for me and but unknown message...");
      sendACK(createFrame(getFrameIdValue(frame), getSenderAddressValue(frame), getReceiverAddressValue(frame), ACK_KO, ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED));
    }
  }
  else {
    printDebug("Message not for me sending message via LoRa");
    sendLoRaPacket(frame);
    waitForAck();
  }
}

void waitForAck() {
 
  unsigned long entry = millis();
  String ackFrame = "";
  
  printDebug("Waiting for ACK...");
  
  while (millis() - entry < WAIT_FOR_ACK_TIMEOUT) {
  //while(1) {
    if (LoRa.parsePacket()) {      
      while (LoRa.available()) {
        ackFrame = ackFrame + ((char)LoRa.read());
      }
      
      //Here a frame has been received !!
      break;
    }      
 }

 if (ackFrame != "") {
    printDebug("Received LoRa ACK : " + ackFrame);
  
     if (!frameSanityCheck(ackFrame)) {           
        printDebug("Bad syntax of received LoRa ACK : " + ackFrame);     
        sendACK(createFrame(UNKNOWN_FRAME_ID, MODULE_ADDRESS, UNKNOWN_RECEIVER_ADDRESS, ACK_KO, ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER));
     }
     else {
        printDebug("Sending ACK to program : " + ackFrame);     
        sendACK(ackFrame);            
     }
 }
 else {   
    printDebug("Timeout waiting for LoRa ACK");
    sendACK(createFrame(UNKNOWN_FRAME_ID, MODULE_ADDRESS, UNKNOWN_RECEIVER_ADDRESS, ACK_KO, ACK_KO_TIMEOUT_WAITING_FOR_ACK));     
 }
}

void sendACK(String ackFrame) {
  Serial.print(ackFrame);
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

void sendLoRaPacket(String frame) {
  // send packet
  LoRa.beginPacket();
  LoRa.print(frame);
  LoRa.endPacket();
}

void purgeSerialBuffer(){

  //Put some delay so data can have time to arrive
  delay(1);

  //Purge data available in buffer...
  while(Serial.available() > 0) {
    char t = Serial.read();
    delay(1);
  }  
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

String getFrameMessageValue(String frame) {
  return getFrameChunk(frame, ';', 4);
}

String getCheckSumValue(String frame) {
  return getFrameChunk(frame, ';', 6);
}

String getReceiverAddressValue(String frame) {
  return getFrameChunk(frame, ';', 3);
}

String getSenderAddressValue(String frame) {
  return getFrameChunk(frame, ';', 2);
}

String getFrameIdValue(String frame) {
  return getFrameChunk(frame, ';', 1);
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
