/**** 
 * kQuatreSender
 * 
 * F. Guiet 
 * Creation           : 25/04/2020
 * Last modification  : 
 * 
 * Version            : 1
 * 
 * History            : 
 *                      
 *                      
 * Librairies used    :
 * 
 *  - https://github.com/sandeepmistry/arduino-LoRa
 *  version 0.7.2
 *  
 *  Arduino IDE used :
 *  
 *  version 1.8.12
 * 
 */

 //** Samples ***

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
//*** @;7;0;2;FIRE+200;1+7;B1;|

//Library needed
#include <SPI.h>
#include <LoRa.h>

//DEBUG MODE
// 0 - Non debug mode
// 1 - debug mode
#define DEBUG 0

//LoRa radio band settings to use
const long FREQ = 868E6;
const int SF = 7;
const long BW = 125E3;

//Frame constants
//const int WAIT_FOR_FRAME_TIMEOUT = 200; //in ms
const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO";
const String PING_ORDER = "PING";
const char START_FRAME_DELIMITER = '@';
const char END_FRAME_DELIMITER = '|';
const String COORDINATOR_MODULE_ADDRESS = "0"; //This module address...should be 0 for coordinator
const String UNKNOWN_RECEIVER_ADDRESS = "0";
const String UNKNOWN_FRAME_ID = "0";
const String ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_KQUATRE_SOFTWARE = "KO_S1";
const String ACK_KO_UNKNOWN_ORDER_RECEIVED_BY_SENDING_MODULE = "KO_S2";
//const String ACK_KO_TIMEOUT_WAITING_FOR_ACK = "KO_S3";
const String ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_FOREIGN_MODULE = "KO_S4";
const String ACK_OK_PONG = "OK_S1";

struct FrameDef {
    String FrameId;    
    String SenderAddress;
    String ReceiverAddress;
    String Message;
};

//Variables
volatile bool is_frame_received = false;
String asynchronousFrameRead = "";
bool is_frame_received_from_k4_software = false;

//************************
//* Initializing program *
//************************
void setup() {

  //Set Serial baudrate
  Serial.begin(115200);
  while (!Serial);

  //Set this according to module you use as sender
  //Leave default settings with Arduino + Dragino
  
  //For instance : here is the configuration to use with Wemos D1 Mini  
  //LoRa.setPins(16, 17, 15); // set CS, reset, IRQ pi

  //Init LoRa
  if (!LoRa.begin(FREQ)) {
    Serial.println("Starting LoRa failed!");
    while (1);
  }
  
  LoRa.setSpreadingFactor(SF);
  LoRa.setSignalBandwidth(BW);

  //Set syncword so other LoRa message does not interfer
  LoRa.setSyncWord(0xEE);   

  //Callback when message is received
  LoRa.onReceive(OnReceive);
  LoRa.receive();

  PrintDebug("Ready...");  
}

/*******************
/* Running program *
/*******************/
void loop() {    

  //Listen for frame to arrive on serial
  AsyncSerialListener();  
  
  if (is_frame_received_from_k4_software) {    
    is_frame_received_from_k4_software = false;    
    FrameHandler(asynchronousFrameRead);
    asynchronousFrameRead = "";
  } 

  if (is_frame_received) {
    is_frame_received = false;
    FrameReceivedHandler();
  }
}

/*********************************************/
/* handle frame received from another module */
/*********************************************/
void FrameReceivedHandler() {

  String ackFrame = "";
  
  while (LoRa.available()) {
     ackFrame = ackFrame + ((char)LoRa.read());
  }

  if (ackFrame != "") {
    PrintDebug("Received LoRa ACK : " + ackFrame);
  
    if (!FrameSanityCheck(ackFrame)) {           
       PrintDebug("Bad syntax of received LoRa ACK : " + ackFrame);                            
       
       String frameToSend = CreateFrameHelper(UNKNOWN_FRAME_ID, COORDINATOR_MODULE_ADDRESS, UNKNOWN_RECEIVER_ADDRESS, ACK_KO, ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_FOREIGN_MODULE);
       SendACK(frameToSend);
    }
    else {
       PrintDebug("Sending ACK to kQuatre software : " + ackFrame);     
       SendACK(ackFrame);            
    }
  }  
}

/*********************************************/
/* handle frame coming from kQuatre software */
/*********************************************/
void FrameHandler(String frame) {
  
  PrintDebug("Frame received : *" + frame + "*");

  bool isFrameValid = FrameSanityCheck(frame);
  
  if (!isFrameValid) {    
    
    PrintDebug("Syntax of received frame KO : " + frame); 
    
    String frameToSend = CreateFrameHelper(UNKNOWN_FRAME_ID, COORDINATOR_MODULE_ADDRESS, UNKNOWN_RECEIVER_ADDRESS, ACK_KO, ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_KQUATRE_SOFTWARE);      
    SendACK(frameToSend);    
    
  }
  else {
    PrintDebug("Frame syntax OK here");    
    
    //Send Lora packet
    SendMessage(frame);    
  }  
}

/***************************************/
/* Listen to serial for frame to send  */
/***************************************/
void AsyncSerialListener() {
 
  // if there's no packet, return
  if (Serial.available() == 0) return;

  //For debugging purpose with when using Arduino IDE to send frame
  char received = Serial.read();
  if (received=='\r' || received=='\n') {  
      return;
  }  

  asynchronousFrameRead.concat(received);

  if (received == END_FRAME_DELIMITER) {
    is_frame_received_from_k4_software = true;
  } 
}

/*************************************
/* Parse frame that has been sanity check before to an FrameDef object *
/*************************************/
FrameDef FrameParser(String frame) {    

  FrameDef frameDef;

  frameDef.FrameId = GetFrameIdValue(frame);
  frameDef.SenderAddress = GetSenderAddressValue(frame);
  frameDef.ReceiverAddress = GetReceiverAddressValue(frame);    
  frameDef.Message = GetFrameMessageValue(frame);   

  return frameDef;
}

/*******************/
/* Sending message */
/*******************/
void SendMessage(String frame) {

  FrameDef frameDef = FrameParser(frame);
  
  //Message sent to me ?
  if (frameDef.ReceiverAddress == COORDINATOR_MODULE_ADDRESS) {
    if (frameDef.Message == PING_ORDER) {
      
      PrintDebug("Message for me and it is PING...sending PONG :)");
      String frameToSend = CreateFrameHelper(frameDef.FrameId, frameDef.SenderAddress, frameDef.ReceiverAddress, ACK_OK, ACK_OK_PONG);      
      SendACK(frameToSend);         
      
    }
    else {      
      
      PrintDebug("Message for me and but unknown message...");
      String frameToSend = CreateFrameHelper(frameDef.FrameId, frameDef.SenderAddress, frameDef.ReceiverAddress, ACK_KO, ACK_KO_UNKNOWN_ORDER_RECEIVED_BY_SENDING_MODULE);
      SendACK(frameToSend);
            
    }
  }
  else {
    
    PrintDebug("Message not for me sending message via LoRa");    
    SendLoRaPacket(frame);       
    
  }
}

/********************************************************/
/* Callback method called when LoRa message is received */
/********************************************************/
void OnReceive(int packetSize) {

  // if there's no packet, return
  if (packetSize == 0) return;   

  //Notify frame received to the main loop
  is_frame_received = true;
}

/****************************/
/* Helper to create a frame */
/****************************/
String CreateFrameHelper(String frameId, String senderAddress, String receiverAddress, String message, String message_complement) {
 
  String frame = String(START_FRAME_DELIMITER) + ";" + frameId + ";" + senderAddress + ";" + receiverAddress + ";" + message + ";" + message_complement;  
  frame.concat(";");
  String checkSum = ComputeCheckSum(frame);
  frame.concat(checkSum);
  frame.concat(";");
  frame.concat(String(END_FRAME_DELIMITER));

  PrintDebug("Frame created : " + frame);  

  return frame;
}

/*********************************/
/* send ACK frame through serial */
/*********************************/ 
void SendACK(String ackFrame) {  
  Serial.print(ackFrame);   
  Serial.flush();
}

/***********************/
/* Sending LoRa packet */
/***********************/
void SendLoRaPacket(String frame) {
  
  PrintDebug("Sending frame : "+frame);
  // send packet
  LoRa.beginPacket();
  LoRa.print(frame);
  LoRa.endPacket();  

  //Back 
  LoRa.receive();
}

bool IsCheckSumValid(String frame) {

  String frameCheckSum = GetCheckSumValue(frame);
  
  PrintDebug("frameCheckSum : " + frameCheckSum);

  //Sanity check
  if (frameCheckSum == "") return false;
  if (frameCheckSum.length() != 2) return false;  

  int lenghtToTake =  frame.length() - 4; 

  if (lenghtToTake <= 0) return false;

  PrintDebug("Lenght To Take : " + String(lenghtToTake));
  
  String trimmedFrame = frame.substring(0, lenghtToTake);

  PrintDebug("Computing debug of : " + trimmedFrame);
  
  if (ComputeCheckSum(trimmedFrame) == frameCheckSum) {
    PrintDebug("Check sum is ok for me");
    return true;
  }

    PrintDebug("Check sum is not ok for me");
  return false;
}

bool FrameSanityCheck(String frame) {

  if (frame.length() == 0) return false;

  //Check checksum is enough to say whether a frame is valid or not... 
  if (IsCheckSumValid(frame))
    return true;
  else
    return false;  
}

String ComputeCheckSum(String frame) {
  
  int lenghtToTake = frame.length();
  
  byte buffer[lenghtToTake + 1]; //lenght + end 
  
  frame.getBytes(buffer,lenghtToTake + 1); 
  
  int checkSum = 0;
  for(int i=0;i <= lenghtToTake - 1; i++) {
    checkSum += buffer[i];
    //PrintDebug(String(buffer[i]));
  }

  PrintDebug("CheckSum : " + String(checkSum));
  
  checkSum &= 0xff;

  String checkSumStr = String(checkSum, HEX);
  checkSumStr.toUpperCase();
  
  if (checkSumStr.length() == 1) {
    checkSumStr = "0" + checkSumStr;
  }

  PrintDebug("CheckSum Str : *" + checkSumStr + "*");

  return checkSumStr;
}

/*String GetFrameAckTimeOutValue(String frame) {

  String message = getFrameChunk(frame, ';', 4);
  
  return GetFrameChunk(message, '+', 1);
}*/

String GetFrameMessageValue(String frame) {

  String message = GetFrameChunk(frame, ';', 4);
  
  return GetFrameChunk(message, '+', 0);
}

String GetCheckSumValue(String frame) {
  return GetFrameChunk(frame, ';', 6);
}

String GetReceiverAddressValue(String frame) {
  return GetFrameChunk(frame, ';', 3);
}

String GetSenderAddressValue(String frame) {
  return GetFrameChunk(frame, ';', 2);
}

String GetFrameIdValue(String frame) {
  return GetFrameChunk(frame, ';', 1);
}

String GetFrameChunk(String data, char separator, int index) 
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

/****************/
/* Debug method */
/****************/
void PrintDebug(String debugMessage) {
 if (DEBUG) {
    Serial.println(debugMessage);
    Serial.flush();
  }  
}
