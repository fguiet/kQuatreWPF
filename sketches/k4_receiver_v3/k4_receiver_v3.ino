/**** 
 * k4_receiver_v3
 * 
 * F. Guiet 
 * Creation           : 29/04/2019
 * Last modification  : 
 * 
 * Version            : 3
 * 
 * History            : 
 *                      
 * Librairies used    :
 * 
 *  - https://github.com/sandeepmistry/arduino-LoRa
 *  version 0.5
 *  
 *  - https://github.com/EinarArnason/ArduinoQueue
 *  version 1.0.1
 * 
 */

#include "Queue.h"
#include <SPI.h>
#include <LoRa.h>


//DEBUG MODE
// 0 - Non debug mode
// 1 - debug mode
#define DEBUG 1

const long FREQ = 868E6;
const int SF = 7;
const long BW = 125E3;

#define NUMBER_OF_RELAY 16 //Nb of relay
#define STATUS_PIN 13
#define OHMETER_PIN 0
#define FIRST_RELAY_DIGITAL_PIN 29 //first digital relay pin minus 1
#define RELAY1_TEST_PIN 22 //Relay pin (test mode)
#define RELAY2_TEST_PIN 23 //Relay pin (test mode)

const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO";
const char START_FRAME_DELIMITER = '@';
const char END_FRAME_DELIMITER = '|';
const int MIN_FRAME_LENGHT = 17; 

const String UNKNOWN_RECEIVER_ADDRESS = "0";
const String SENDER_MODULE_ADDRESS = "0";
const String UNKNOWN_FRAME_ID = "0";
const String ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER = "KO_R1";
const String ACK_OK_FRAME_RECEIVED = "OK_R1";


struct frameObj {
  String frame;
  int rssi;
  int snr;
  bool frameOk;
};
Queue<frameObj> frameQueue;


/***
 * !!! Modify this !!!
 */
const String MODULE_ADDRESS = "1";


void printDebug(String debugMessage) {
 if (DEBUG) {
    Serial.println(debugMessage);
  }  
}

void setup() {

  //Set Serial baudrate, only when debugging!
  if (DEBUG) {
    Serial.begin(115200);
    while (!Serial);
  }

  //Init LoRa
  if (!LoRa.begin(FREQ)) {
    Serial.println("Starting LoRa failed!");
    while (1);
  }

  LoRa.setSpreadingFactor(SF);
  LoRa.setSignalBandwidth(BW);
  
  //Set syncword so other LoRa message does not interfer
  LoRa.setSyncWord(0xEE); 

  //Set Transmit powser to 23db (17 is default)
  LoRa.setTxPower(23);

  // register the receive callback
  LoRa.onReceive(onReceive);

  // put the radio into receive mode
  LoRa.receive();
  
  if (!DEBUG) {
    initRelays();
  }

  //Init Pin status 
  pinMode(STATUS_PIN, OUTPUT);
  //Turn off pin  
  digitalWrite(STATUS_PIN, LOW);
  
  printDebug("Ready...");
  
}

void loop() {

  if (!frameQueue.isEmpty()) {
    frameObj frame = frameQueue.dequeue();

    handleFrameMessage(frame.frame, frame.rssi, frame.snr);
  }
  
}

void sendLoRaPacket(String frame) {

  //For testing purpose...
  //frame = "@;211;3;0;ACK_OK;OK_R3;DD;|";

  printDebug("Sending LoRa frame :  " + frame);  
  
  // send packet
  LoRa.beginPacket();
  LoRa.print(frame);
  LoRa.endPacket();

  //Go back in receive mode
  LoRa.receive();
}

void onReceive(int packetSize) {

  if (packetSize==0) return;

  String frameStr = "";
  
  while (LoRa.available()) {
      frameStr = frameStr + ((char)LoRa.read());
    }

  //Get Rssi
  int rssi = LoRa.packetRssi();

  //Get Snr
  int snr = LoRa.packetSnr();

  //Handle
  handleReceivedFrame(frameStr, rssi, snr); 
}

void handleReceivedFrame(String frameStr, int rssi, int snr) {

  bool isFrameOk = frameSanityCheck(frameStr);

  //Check received frame
  if (!isFrameOk) {          

    printDebug("bad syntax of trame received :  " + frameStr); 
        
    //Bad frame received
    sendLoRaPacket(createFrame(UNKNOWN_FRAME_ID, MODULE_ADDRESS, SENDER_MODULE_ADDRESS, ACK_KO, ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER));
  }
  else {

    printDebug("syntax of trame received is ok :  " + frameStr);  
    
    //Frame ok here let's handle frame message
    //Let's see whether frame is for me...
    String receiverAddress = getReceiverAddressValue(frameStr);
    String message = getFrameMessageValue(frameStr);
    
    if (receiverAddress == MODULE_ADDRESS) {

      printDebug("trame received is for me :  " + frameStr);  

      if (message != "OHM") {
        //Send always ACK here ... except when message is OHM ... will send ACK later
        sendLoRaPacket(createFrame(getFrameIdValue(frameStr), MODULE_ADDRESS, getSenderAddressValue(frameStr), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr)));    
      }
      
      frameObj frame = { frameStr, rssi, snr, isFrameOk };

      frameQueue.enqueue(frame);
    }
  } 
}

String GetResistance(String frame) {

  String messageComplement = getFrameMessageCompValue(frame);

  printDebug("Channel a tester : "+messageComplement);

  int relayToCheck = messageComplement.toInt();
  
  //Active test relay mode
  digitalWrite(RELAY1_TEST_PIN,HIGH);
  digitalWrite(RELAY2_TEST_PIN,HIGH); 

  //Wait a litlle
  delay(500);

  digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToCheck, LOW); //Le relay 1 doit être branché sur le digital 30 et ainsi de suite
  
  //Wait a little
  delay(500);
    
  //Read resistance
  float resistance = ComputeResistance();
                            
  //Deactive test relay mode
  digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToCheck, HIGH);        

  //Wait a little
  //delay(500);
       
  //Deactivate test mode
  digitalWrite(RELAY1_TEST_PIN,LOW); 
  digitalWrite(RELAY2_TEST_PIN,LOW);    
      
  char valBuffer[10];
            
  memset(valBuffer,'\0',10);
    
  dtostrf(resistance,4,4,valBuffer);
  
  String result(valBuffer);

  printDebug("Result resistance : "+result);

  //result.replace(".",",");
  
  return result;  
}

float ComputeResistance() {
  
  int raw= 0;
  int Vin= 5; //Voltage en entrée 5V
  float Vout= 0;
  float R1= 47; //47 Ohm pour la résistance que l'on connait (100Ohm pour celle que l'on mesure)
  float R2= 0;
  float Buffer= 0;
  
  raw=analogRead(OHMETER_PIN);

  printDebug("Raw : " + String(raw));
    
  if (raw) 
  {  
     Buffer= raw * Vin;
     Vout= (Buffer)/1024.0;
     Buffer= (Vin/Vout) -1;
     R2= R1 * Buffer; 
  }  
  
  return R2;
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

void handleFrameMessage(String frame, int rssi, int snr)  {

  String message = getFrameMessageValue(frame);

  printDebug("message received :  " + message);  

  if (message == "INIT") {
    initRelays();
  }

  if (message == "PING") {
    //Nothing to do here :)
    return;    
  }

  if (message == "OHM") {
    String result = GetResistance(frame);    
    //Send ACK with OHM mesurement
    sendLoRaPacket(createFrame(getFrameIdValue(frame), MODULE_ADDRESS, getSenderAddressValue(frame), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr) + "+" + result));    
  }
 
  if (message == "FIRE") { //Ok let's fire!!
    fireFirework(frame);                        
  }

  //Handle message unknown here?
}

void fireFirework(String frame) {

  String messageComplement = getFrameMessageCompValue(frame);

  printDebug("Firework message complement : " + messageComplement);
  
  //On indique que l'on a recu quelque chose
  digitalWrite(STATUS_PIN, HIGH);
    
  //Serial.println("Chunk 2 : "+chunk2);
  int nbOfRelayToFire = getFrameChunk(messageComplement,'+',0).toInt();
  
  //Activation des relays! feu!!
  int chunkNumber = 1;
  for(int i=0;i<nbOfRelayToFire;i++) 
  {          
     int relayToFire = getFrameChunk(messageComplement,'+',chunkNumber + i).toInt(); //Numéro de relay à déclencher (1..16)
     printDebug("Activating relay : " + String(relayToFire));
     digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToFire, LOW); //Le relay 1 doit être branché sur le digital 30 et ainsi de suite
  }
  
  //On patiente un peu...
  delay(1000);
  delay(1000);
  
  //Désactivation des relays pour éviter les courts-circuit
  for(int i=0;i<nbOfRelayToFire;i++) 
  {                    
     int relayToFire = getFrameChunk(messageComplement,'+',chunkNumber + i).toInt(); //Numéro de relay à déclencher (1..16)
     printDebug("De-activating relay : " + String(relayToFire));
     digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToFire, HIGH); //Le relay 1 doit être branché sur le digital 22 et ainsi de suite
  }
  
  //Terminé on eteint la led
  digitalWrite(STATUS_PIN, LOW);
}

bool isCheckSumValid(String frame) {

  String frameCheckSum = getCheckSumValue(frame);

  printDebug("frameCheckSum : " + frameCheckSum);

  //Sanity check
  if (frameCheckSum == "") return false;
  if (frameCheckSum.length() != 2) return false;

  int lenghtToTake =  frame.length() - 4; 
  String trimmedFrame = frame.substring(0, lenghtToTake);
  
  if (ComputeCheckSum(trimmedFrame) == frameCheckSum) return true;

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

  printDebug("CheckSum Str : " + checkSumStr);

  return checkSumStr;
}

String getFrameAckTimeOutValue(String frame) {

  String message = getFrameChunk(frame, ';', 4);

  return getFrameChunk(message, '+', 1);
}

String getFrameMessageValue(String frame) {

  String message = getFrameChunk(frame, ';', 4);

  return getFrameChunk(message, '+', 0);
}

String getFrameMessageCompValue(String frame) {
  return getFrameChunk(frame, ';', 5);
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

int countCharInString(String message, char toFind) {
   int i, count;
   for (i=0, count=0; message[i]; i++)
     count += (message[i] == toFind);

   return count;
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
