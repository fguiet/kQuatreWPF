/**** 
 * kQuatreReceiver
 * 
 * F. Guiet 
 * Creation           : 29/04/2019
 * Last modification  : 26/04/2020
 * 
 * Version            : 5
 * 
 * History            : 
 *                      
 *                      3.1 : 26/05/2019 : Remove Enqueue library (not working very well)              
 *                      5   : 26/05/2019 : Add asynchronous fire handling
 *                      5.1 : ??/04/2020 : Update LoRa library to 0.7.2
 *                      5.2 : 25/04/2020 : Remove LoRa.setTxPower(23);
 *                      
 * Librairies used    :
 * 
 *  - https://github.com/sandeepmistry/arduino-LoRa
 *  version 0.7.2
 *   
 * 
 */
#include <SPI.h>
#include <LoRa.h>

//DEBUG MODE
// 0 - Non debug mode
// 1 - debug mode
#define DEBUG 0

#define VERSION "2020/04/25 - v5.2 - LoRa 0.7.2"

const long FREQ = 868E6;
const int SF = 7;
const long BW = 125E3;

#define NUMBER_OF_RELAY 16 //Nb of relay
#define STATUS_PIN 13
#define VOLTAGE_PIN A0
#define FIRST_RELAY_DIGITAL_PIN 29 //first digital relay pin minus 1
#define RELAY1_TEST_PIN 22 //Relay pin (test mode)
#define RELAY2_TEST_PIN 23 //Relay pin (test mode)

const int SHUTDOWN_RELAY_TIMEOUT = 2000; //in ms
const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO";
const char START_FRAME_DELIMITER = '@';
const char END_FRAME_DELIMITER = '|';
const int MIN_FRAME_LENGHT = 17; 

const String UNKNOWN_RECEIVER_ADDRESS = "0";
const String SENDER_MODULE_ADDRESS = "0";
const String UNKNOWN_FRAME_ID = "0";
const String ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_COORDINATOR = "KO_R1";
const String ACK_OK_FRAME_RECEIVED = "OK_R1";


struct frameObj {
  String frame;
  int rssi;
  int snr;
  bool isReceived;
};

struct fireworkObj {  
  unsigned long inFireStateMS = 0;  
  bool toFire = false;
};

fireworkObj fireworks[NUMBER_OF_RELAY];

frameObj frame = { "", 0, 0, false };

/***
 * !!! Modify this !!!
 */
const String MODULE_ADDRESS = "2";

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
  //LoRa.setTxPower(23);

  // register the receive callback
  LoRa.onReceive(onReceive);

  // put the radio into receive mode
  LoRa.receive();
  
  //if (!DEBUG) {
  initRelays();
  //}

  //Init Pin status 
  pinMode(STATUS_PIN, OUTPUT);
  //Turn off pin  
  digitalWrite(STATUS_PIN, LOW);
  
  printDebug("Ready...");
  
}

void loop() {

  if (frame.isReceived) {
    frame.isReceived = false;
    printDebug("Handle frame : " + frame.frame);
    handleFrameMessage(frame.frame, frame.rssi, frame.snr);    
  } 

  asynchronousFireManagement();
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

      if (message != "COND" && message != "INFO") {
        //Send always ACK here ... except when message is COND ... will send ACK later
        sendLoRaPacket(createFrame(getFrameIdValue(frameStr), MODULE_ADDRESS, getSenderAddressValue(frameStr), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr)));    
      }
      
      //frameObj frame = { frameStr, rssi, snr, isFrameOk };
      frame = { frameStr, rssi, snr, true };     
    }
  } 
}

String GetConductivite(String frame) {

  String messageComplement = getFrameMessageCompValue(frame);

  printDebug("Channel a tester : "+messageComplement);

  int relayToCheck = messageComplement.toInt();
  
  //Active test relay mode
  digitalWrite(RELAY1_TEST_PIN,HIGH);
  digitalWrite(RELAY2_TEST_PIN,HIGH); 

  //Wait a litlle
  delay(250);

  digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToCheck, LOW); //Le relay 1 doit être branché sur le digital 30 et ainsi de suite
  
  //Wait a little
  delay(250);
    
  //Read conductivity
  String conductivity = ComputeConductivite();

  //Wait a litlle
  delay(250);
                            
  //Deactive test relay mode
  digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToCheck, HIGH);        

  //Wait a little
  delay(250);
       
  //Deactivate test mode
  digitalWrite(RELAY1_TEST_PIN,LOW); 
  digitalWrite(RELAY2_TEST_PIN,LOW);    
      
  //char valBuffer[10];
            
  //memset(valBuffer,'\0',10);
    
  //dtostrf(voltage,4,4,valBuffer);
  
  //String result(valBuffer);

  //printDebug("Result voltage : "+result);
  
  return conductivity;  
}

String ComputeConductivite() {

   //5v = 1023
  //https://www.arduino.cc/reference/en/language/functions/analog-io/analogread/
  
  //int raw=analogRead(VOLTAGE_PIN);
  

  int conductivite = digitalRead(3);
  
  printDebug("Conductivite : " + String(conductivite));

  if (conductivite == HIGH) {
    return "NON";
  }
  else {
    return "OUI";
  }

  //float voltage = raw * 5 / 1024.0; //.0 is important otherwise integer value is return...
  
  //printDebug("Raw : " + String(raw));
  //printDebug("Voltage : " + String(voltage));

  //return voltage;
  
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

  if (message == "INFO") {    
    sendLoRaPacket(createFrame(getFrameIdValue(frame), MODULE_ADDRESS, getSenderAddressValue(frame), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr) + "+" + String(VERSION)+ " - Debug : " + String(DEBUG))); 
  }

  if (message == "PING") {
    //Nothing to do here :)
    return;    
  }

  if (message == "COND") {
    String result = GetConductivite(frame);    
    //Send ACK with COND mesurement
    sendLoRaPacket(createFrame(getFrameIdValue(frame), MODULE_ADDRESS, getSenderAddressValue(frame), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr) + "+" + result));    
  }
 
  if (message == "FIRE") { //Ok let's fire!!
    fireFirework(frame);                        
  }

  //Handle message unknown here?
}

void asynchronousFireManagement() {
  //Check relay to fire!
  for(int i=0;i<NUMBER_OF_RELAY;i++) {
    if (fireworks[i].toFire) {
      fireworks[i].toFire = false;      
      fireworks[i].inFireStateMS = millis();      
      printDebug("Activating relay : " + String(i+1));
      digitalWrite(FIRST_RELAY_DIGITAL_PIN + i + 1, LOW); //Le relay 1 doit être branché sur le digital 30 et ainsi de suite
    }

    //unsigned long entry = millis();
    if (fireworks[i].inFireStateMS !=0 && (millis() - fireworks[i].inFireStateMS) > SHUTDOWN_RELAY_TIMEOUT) {
      fireworks[i].inFireStateMS = 0;
      printDebug("De-activating relay : " + String(i+1));
      digitalWrite(FIRST_RELAY_DIGITAL_PIN + i + 1, HIGH); //Le relay 1 doit être branché sur le digital 22 et ainsi de suite
    }
  }  
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
    fireworks[relayToFire - 1].toFire = true;    
  }
  
  //Activation des relays! feu!!
  /*int chunkNumber = 1;
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
  }*/
  
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

    //Pour les tests de conductivité
    pinMode(3,INPUT);
  
    for(int i=1;i<=NUMBER_OF_RELAY;i++) {
       pinMode(FIRST_RELAY_DIGITAL_PIN + i, OUTPUT);
       fireworks[i-1].toFire = false;
       fireworks[i-1].inFireStateMS = 0;
    } 
    
    //Relay pour le circuit de test (conductivité)
    pinMode(RELAY1_TEST_PIN, OUTPUT);  
    pinMode(RELAY2_TEST_PIN, OUTPUT);  
    
    for(int i=1;i<=NUMBER_OF_RELAY;i++) {
        digitalWrite(FIRST_RELAY_DIGITAL_PIN + i,HIGH); //Eteint les relays
    } 
    
    //Relay pour le circuit de test (conductivité)         
    digitalWrite(RELAY1_TEST_PIN,LOW); //Eteint les relays
    digitalWrite(RELAY2_TEST_PIN,LOW); //Eteint les relays
}
