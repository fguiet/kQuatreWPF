#include <SPI.h>
#include <LoRa.h>

//DEBUG MODE
#define DEBUG 0

const long FREQ = 868E6;
const int SF = 9;
const long BW = 125E3;

#define NUMBER_OF_RELAY 16 //Nb of relay
#define STATUS_PIN 13
#define OHMETER_PIN 0
#define FIRST_RELAY_DIGITAL_PIN 29 //first digital relay pin minus 1
#define RELAY1_TEST_PIN 52 //Relay pin (test mode)
#define RELAY2_TEST_PIN 53 //Relay pin (test mode)

const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO";
const char START_FRAME_DELIMITER = '@';
const char END_FRAME_DELIMITER = '|';
const int MIN_FRAME_LENGHT = 17; 

const String UNKNOWN_RECEIVER_ADDRESS = "-1";
const String SENDER_MODULE_ADDRESS = "0";
const String UNKNOWN_FRAME_ID = "-1";
const String ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER = "KO_R1";
const String ACK_OK_FRAME_RECEIVED = "OK_R1";

/***
 * !!! Modify this !!!
 */
const String MODULE_ADDRESS = "3";

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

  //Init relays
  initRelays();

  //Init Pin status 
  pinMode(STATUS_PIN, OUTPUT);
  //Turn off pin  
  digitalWrite(STATUS_PIN, LOW);
  
  printDebug("Ready...");

}

void loop() {

  String frame = "";
  
  if (LoRa.parsePacket()) {      
    
    while (LoRa.available()) {
      frame = frame + ((char)LoRa.read());
    }
    
    printDebug("Trame received : " + frame);  
    
    handleReceivedFrame(frame);
  }  
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

  printDebug("CheckSum Str : " + checkSumStr);

  return checkSumStr;
}

String createFrame(String frameId, String senderAddress, String receiverAddress, String message, String message_complement) {
  
  String frame = String(START_FRAME_DELIMITER) + ";" + frameId + ";" + senderAddress + ";" + receiverAddress + ";" + message + ";" + message_complement;
  String checkSum = ComputeCheckSum(frame);
  frame.concat(";");
  frame.concat(checkSum);
  frame.concat(";");
  frame.concat(String(END_FRAME_DELIMITER));

  printDebug("Frame created : " + frame);  

  return frame;
}

int countCharInString(String message, char toFind) {
   int i, count;
   for (i=0, count=0; message[i]; i++)
     count += (message[i] == toFind);

   return count;
}


void handleReceivedFrame(String frame) {

  //Check received frame
  if (!frameSanityCheck(frame)) {          

    printDebug("bad syntax of trame received :  " + frame);  
    
    //Bad frame received
    sendLoRaPacket(createFrame(UNKNOWN_FRAME_ID, MODULE_ADDRESS, SENDER_MODULE_ADDRESS, ACK_KO, ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER));
  }
  else {

    printDebug("syntax of trame received is ok :  " + frame);  
    
    //Frame ok here let's handle frame message
    //Let's see whether frame is for me...
    String receiverAddress = getReceiverAddressValue(frame);
    if (receiverAddress == MODULE_ADDRESS) {

      printDebug("trame received is for me :  " + frame);  
      
      //SendACK OK here, everything!!
      sendLoRaPacket(createFrame(getFrameIdValue(frame), MODULE_ADDRESS, getSenderAddressValue(frame), ACK_OK, ACK_OK_FRAME_RECEIVED));    
      
      handleFrameMessage(frame);
    }
  } 
}

void handleFrameMessage(String frame)  {

  String message = getFrameMessageValue(frame);

  printDebug("message received :  " + message);  

  if (message == "INIT") {
    initRelays();
  }

  /*if (message == "OHM") {
    int relayToCheck = getValue(payload,';',5).toInt();
    checkResistance(relayToCheck);
  }     

  if (message == "FIRE") { //Ok let's fire!!
    fireFirework(payload);                        
  } */   
}

bool isCheckSumValid(String frame) {

  String frameCheckSum = getCheckSumValue(frame);

  printDebug("frameCheckSum : " + frameCheckSum);

  //Sanity check
  if (frameCheckSum == "") return false;
  if (frameCheckSum.length() != 2) return false;

  int lenghtToTake =  frame.length() - 5; 
  String trimmedFrame = frame.substring(0, lenghtToTake);
  
  if (ComputeCheckSum(trimmedFrame) == frameCheckSum) return true;

  return false;
}

bool frameSanityCheck(String frame) {

  printDebug("Nb of ; : " + String(countCharInString(frame,';')));

  //At least 6 ;  
  if (countCharInString(frame,';') != 6) return false;
  
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

void sendLoRaPacket(String frame) {

  printDebug("Sending LoRa frame :  " + frame);  
  
  // send packet
  LoRa.beginPacket();
  LoRa.print(frame);
  LoRa.endPacket();
}

String getFrameMessageValue(String frame) {
  return getFrameChunk(frame, ';', 4);
}

String getCheckSumValue(String frame) {
  return getFrameChunk(frame, ';', 5);
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

  //Nothing found !! return empty string !!
  return "";
}

void printDebug(String debugMessage) {
 if (DEBUG) {
    Serial.println(debugMessage);
  }  
}

/*
* Relay initialisation
*/
void initRelays() {

    printDebug("Init relays...");
  
    for(int i=1;i<=NUMBER_OF_RELAY;i++) {
       pinMode(FIRST_RELAY_DIGITAL_PIN + i, OUTPUT);
    } 
    
    //Relay pour le circuit de test (ohmètre)
    pinMode(RELAY1_TEST_PIN, OUTPUT);  
    pinMode(RELAY2_TEST_PIN, OUTPUT);  
    
    for(int i=1;i<=NUMBER_OF_RELAY;i++) {
        digitalWrite(FIRST_RELAY_DIGITAL_PIN + i,HIGH); //Eteint les relays
    } 
    
    //Relay pour le circuit de test (ohmètre)         
    digitalWrite(RELAY1_TEST_PIN,LOW); //Eteint les relays
    digitalWrite(RELAY2_TEST_PIN,LOW); //Eteint les relays
}
