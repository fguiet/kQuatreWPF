#include <SPI.h>
#include <LoRa.h>

#define DEBUG 1

#define NUMBER_OF_RELAY 16 //Nb of relay
#define STATUS_PIN 13
#define OHMETER_PIN 0
#define FIRST_RELAY_DIGITAL_PIN 29 //first digital relay pin minus 1
#define RELAY1_TEST_PIN 52 //Relay pin (test mode)
#define RELAY2_TEST_PIN 53 //Relay pin (test mode)

const long freq = 868E6;
const int SF = 9;
const long bw = 125E3;
const String RECEIVER_ID = "1";
const char BEGIN_TRAME = '@';

void setup() {

  Serial.begin(9600);
  while (!Serial);

  if (DEBUG)
    Serial.println("K4 Receiver");

  if (!LoRa.begin(freq)) {
    Serial.println("Starting LoRa failed!");
    while (1);
  }

  LoRa.setSpreadingFactor(SF);
  LoRa.setSignalBandwidth(bw); 
  
  if (DEBUG) {
     Serial.print("Frequency ");
     Serial.print(freq);
     Serial.print(" Bandwidth ");
     Serial.print(bw);
     Serial.print(" SF ");
     Serial.println(SF);     
  }

  if (DEBUG) {
    Serial.println("Ready...");     
  }
  
  //Relays initiation
  initRelays();

  //Pin status init
  pinMode(STATUS_PIN, OUTPUT);
  //Turn of pin  
  digitalWrite(STATUS_PIN, LOW);
}

void sendAck(String message) {
  int check = 0;
  for (int i = 0; i < message.length(); i++) {
    check += message[i];
  }
  // Serial.print("/// ");
  LoRa.beginPacket();
  LoRa.print(String(check));
  LoRa.endPacket();

  if (DEBUG) {  
    Serial.print(message);
    Serial.print(" ");
    Serial.print("Ack Sent: ");
    Serial.println(check); 
  }
}

/*
 * Helper function to chunk String
 */
String getValue(String data, char separator, int index) 
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
}

/*
* Relay initialisation
*/
void initRelays() {
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

void fireFirework(String message) {
  //On indique que l'on a recu quelque chose
  digitalWrite(STATUS_PIN, HIGH);
    
  //Serial.println("Chunk 2 : "+chunk2);
  int nbOfRelayToFire = getValue(message,';',5).toInt();
  
  //Activation des relays! feu!!
  int chunkNumber = 6;
  for(int i=0;i<nbOfRelayToFire;i++) 
  {     
     int relayToFire = getValue(message,';',chunkNumber + i).toInt(); //Numéro de relay à déclencher (1..16)
     digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToFire, LOW); //Le relay 1 doit être branché sur le digital 30 et ainsi de suite
  }
  
  //On patiente un peu...
  delay(1000);
  
  //Désactivation des relays pour éviter les courts-circuit
  for(int i=0;i<nbOfRelayToFire;i++) 
  {                    
     int relayToFire = getValue(message,';',chunkNumber + i).toInt(); //Numéro de relay à déclencher (1..16)
     digitalWrite(FIRST_RELAY_DIGITAL_PIN + relayToFire, HIGH); //Le relay 1 doit être branché sur le digital 22 et ainsi de suite
  }
  
  //Terminé on eteint la led
  digitalWrite(STATUS_PIN, LOW);
}

void checkResistance(int relayToCheck) {
  
  //String chunk2 = getValue(payloadString,';',1);
  // Serial.println("Chunk 2 : "+chunk2);
  //int relayToFire = chunk2.toInt(); //Numéro de relay à déclencher (1..16)
   
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
  delay(500);
       
  //Deactivate test mode
  digitalWrite(RELAY1_TEST_PIN,LOW);
  digitalWrite(RELAY2_TEST_PIN,LOW);    

    //On envoie la valeur lue au xbee emetteur
    //char Buffer[sizeof(resistance)];
   // memcpy(Buffer, &resistance, sizeof(resistance));
    
  char Buffer[100]="";
  char charVal[10];
  //char * Ligne =(char)relayToCheck;
              
  memset(charVal,'\0',100);
  memset(Buffer,'\0',10); //Initialisation            
  strcat(Buffer,"OHM;");
  strcat(Buffer,String(relayToCheck).c_str());
  strcat(Buffer,";");
  dtostrf(resistance,4,4,charVal);
  strcat(Buffer,charVal);  

  //Send message here!!
}

float ComputeResistance() {
  
  int raw= 0;
  int Vin= 5; //Voltage en entrée 5V
  float Vout= 0;
  float R1= 47; //47 Ohm pour la résistance que l'on connait (100Ohm pour celle que l'on mesure)
  float R2= 0;
  float Buffer= 0;
  
  raw=analogRead(OHMETER_PIN);
   Serial.print("Raw: ");
   Serial.println(raw);
  if (raw) 
  {  
     Buffer= raw * Vin;
     Vout= (Buffer)/1024.0;
     Buffer= (Vin/Vout) -1;
     R2= R1 * Buffer;
   //  Serial.print("Vout: ");
   //  Serial.println(Vout);
   //  Serial.print("R2: ");
  //   Serial.println(R2);    
  }  
  
  return R2;
}

void loop() {
  
  int packetSize = LoRa.parsePacket();
  if (packetSize) {
    // received a packet
    String payload = "";
    while (LoRa.available()) {
      payload = payload + ((char)LoRa.read());      
    }

    if (DEBUG)
      Serial.println("payload : " + payload);

    //First check if the trame begins with the correct character
    if (payload.length() >= 1 && payload.charAt(0) == BEGIN_TRAME) {

      //Message description
      String frameId = getValue(payload,';',1);
      String senderAddress = getValue(payload,';',2);
      String receiverAddress = getValue(payload,';',3);

      //is this message for me ?
      if (receiverAddress == RECEIVER_ID) {
      
        //Ok send acknownledgement
        sendAck(payload);  

        String order = getValue(payload,';',4);

        if (DEBUG) {
          Serial.println("Received order : "+order+" from sender with id : "+senderAddress);    
        }

        if (order == "INIT") {
            initRelays();
        }

        if (order == "OHM") {
          int relayToCheck = getValue(payload,';',5).toInt();
          checkResistance(relayToCheck);
        }     

        if (order == "FIRE") { //Ok let's fire!!
          fireFirework(payload);                        
        }  
      }     
    }
    else {
      if (DEBUG) 
        Serial.println("Received something but it doesn't seem to be a trame...");
    }    
  }
}


