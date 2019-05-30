/**** 
 * k4_receiver_v4
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
#include <RHReliableDatagram.h>
#include <RH_RF95.h>
#include <SPI.h>
#define COORDINATOR_ADDRESS 0
#define RECEIVER_ADDRESS 1

// According to LoRa shield (see wiki)
#define RFM95_CS 10
#define RFM95_RST 9
#define RFM95_INT 2

// Change to 868.0Mhz or other frequency, must match RX's freq!
#define RF95_FREQ 868.0

//DEBUG MODE
// 0 - Non debug mode
// 1 - debug mode
#define DEBUG 0

#define NUMBER_OF_RELAY 16 //Nb of relay
#define STATUS_PIN 13
#define OHMETER_PIN 0
#define FIRST_RELAY_DIGITAL_PIN 29 //first digital relay pin minus 1
#define RELAY1_TEST_PIN 22 //Relay pin (test mode)
#define RELAY2_TEST_PIN 23 //Relay pin (test mode)

// Singleton instance of the radio driver
RH_RF95 driver(RFM95_CS, RFM95_INT);

// Class to manage message delivery and receipt, using the driver declared above
RHReliableDatagram manager(driver, RECEIVER_ADDRESS);

// Dont put this on the stack:
uint8_t buf[RH_RF95_MAX_MESSAGE_LEN];

void setup() {
 
  Serial.begin(115200);
  while (!Serial) ;
  
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

  initRelays();
  printDebug("Receiver n°"+String(RECEIVER_ADDRESS)+ ". Ready !");
}

void loop() {

  if (manager.available())  {
    // Wait for a message addressed to us from the client
    uint8_t len = sizeof(buf);
    uint8_t from;
    uint8_t to;

    //Valid message arrived for me? then send ACK back to SRC
    //See https://www.airspayce.com/mikem/arduino/RadioHead/classRHReliableDatagram.html
    if (manager.recvfromAck(buf, &len, &from, &to)) {
      
      //Check message comes from coordinator
      if (COORDINATOR_ADDRESS == from) {
        printDebug("Got a message from my lovely coordinator :)");

        //Check weither message is for me or not...it should with reliable messaging
        if(RECEIVER_ADDRESS == to) {
          printDebug("Frame received from coordinator : " + String((char*)buf));
          handleReceivedFrame((char*)buf);
        }
      }      
    }  
  }  
}

String getFrameMessageValue(String frame) {

  String message = getFrameChunk(frame, ';', 4);

  return getFrameChunk(message, '+', 0);
}

void handleReceivedFrame(String frame) {
  
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
    //TODO : send specific message here
    //Send ACK with OHM mesurement
    //sendLoRaPacket(createFrame(getFrameIdValue(frame), MODULE_ADDRESS, getSenderAddressValue(frame), ACK_OK, ACK_OK_FRAME_RECEIVED + "+" + String(rssi) + "+" + String(snr) + "+" + result));    
  }
 
  if (message == "FIRE") { //Ok let's fire!!
    fireFirework(frame);                        
  }

  //Handle message unknown here?
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

String getFrameMessageCompValue(String frame) {
  return getFrameChunk(frame, ';', 5);
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

void printDebug(String debugMessage) {
 if (DEBUG) {
    Serial.println(debugMessage);
  }  
}
