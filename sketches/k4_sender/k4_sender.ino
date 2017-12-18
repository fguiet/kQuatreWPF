// pin definitions
#define I2C_ADDRESS 0x3C

#include "SSD1306Ascii.h"
#include "SSD1306AsciiAvrI2c.h"

SSD1306AsciiAvrI2c oled;
#include <SPI.h>
#include <LoRa.h>

#define DEBUG 0

const long freq = 868E6;
const int SF = 9;
const int RETRY_MESSAGE_SENDING = 2;
//In Ms
const int waitForAck = 200;
const long bw = 125E3;

const String ACK_OK = "ACK_OK";
const String ACK_KO = "ACK_KO"; 
const char END_FRAME = '|';
const int MAX_FRAME_LENGTH = 50;
const String RECEIVER_ADDRESS = "0";
const char FRAME_SEPARATOR = ';';

int counter = 1, messageLostCounter = 0;

void setup() {

  //Set Serial baudrate
  Serial.begin(9600);
  while (!Serial);

  oled.begin(&Adafruit128x64, I2C_ADDRESS);
  oled.setFont(Adafruit5x7);
  oled.clear();  
  //delay(2000);
  
  //InitScreen();

  //Set LoRa Pins to work with Wemos
  //LoRa.setPins(16, 17, 15); // set CS, reset, IRQ pin

  if (DEBUG)
    Serial.println("K4 Sender");

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

     Serial.println("Ready...");  
  }
}

void displayOnScreen(String message) {    
  
  oled.println(message);
    
  if (DEBUG) {
    Serial.println("Message a afficher : ");
    Serial.println(message);
  }
}

void loop() {
  
  //If nothing availbale on Serial just return...
  if (!Serial.available()) return;

  //First Read Frame from Serial
  String payload = "";
  int frameLenght = 0;  

  while (frameLenght <= MAX_FRAME_LENGTH) {
    if (Serial.available()) {
      char received = Serial.read();
      payload.concat(received); 

      frameLenght++;
    
      if (received == END_FRAME) 
        break;
    }
    //delay(10);
  }

  counter++;
  
  displayOnScreen(String(counter) + " : " + payload);
  
  
  //if (payload == "") return;
 
  if (DEBUG) {
    Serial.print("Received: ");
    Serial.print(payload); 
  }

  String frameId = getValue(payload, FRAME_SEPARATOR, 1);
  String senderAddress = getValue(payload, FRAME_SEPARATOR, 2);
  //Parse message here...to get if message receiver address
  String receiverAddress = getValue(payload, FRAME_SEPARATOR, 3);
  
  //payload is for me :)
  if (receiverAddress == RECEIVER_ADDRESS) {
    String order = getValue(payload, FRAME_SEPARATOR, 4);
    if (order == "PING") {
      sendAck(true, frameId, receiverAddress, senderAddress);
    }
    else {
      sendAck(false, frameId, receiverAddress, senderAddress);
    }

    //Done here!
    return;
  }
   
  //Will do that later
  sendMessage(payload);
  
  int nackCounter = 1;
  while (!receiveAck(payload) && nackCounter <= RETRY_MESSAGE_SENDING) {

    if (DEBUG) {
       Serial.println("Refused...");
       Serial.print(nackCounter);
    }

    //Send message again...
    sendMessage(payload);
    nackCounter++;
  }

  if (nackCounter >= RETRY_MESSAGE_SENDING) {
    
    if (DEBUG) {
      Serial.println("");
      Serial.println("--------------- MESSAGE LOST ---------------------");
    }    

    sendAck(false, frameId, senderAddress, receiverAddress);
  } else {
    if (DEBUG)
      Serial.println("Acknowledged...");

    sendAck(true, frameId, senderAddress, receiverAddress);
  }  
}

void sendAck(bool isOk, String frameId, String senderId, String receiverId) {
  if (isOk) {
    Serial.print("@;" + frameId + ";" + receiverId + ";" + senderId + ";" + ACK_OK + ";|");
    displayOnScreen("@;" + frameId + ";" + receiverId + ";" + senderId + ";" + ACK_OK + ";|");
  }
  else {
    Serial.print("@;" + frameId + ";" + receiverId + ";" + senderId + ";" + ACK_KO + ";|");
    displayOnScreen("@;" + frameId + ";" + receiverId + ";" + senderId + ";" + ACK_KO + ";|");
  }
}

bool receiveAck(String payload) {
  String ack;

  if (DEBUG)  
    Serial.print(" Waiting for Ack ");
  
  bool stat = false;
  unsigned long entry = millis();
  while (stat == false && millis() - entry < waitForAck) {
    if (LoRa.parsePacket()) {
      ack = "";
      while (LoRa.available()) {
        ack = ack + ((char)LoRa.read());
      }
      int check = 0;
      // Serial.print("///");
      for (int i = 0; i < payload.length(); i++) {
        check += payload[i];
        //   Serial.print(payload[i]);
      }
      /*    Serial.print("/// ");
          Serial.println(check);
          Serial.print(message);*/
      if (DEBUG) {
        Serial.print(" Ack ");
        Serial.print(ack);
        Serial.print(" Check ");
        Serial.print(check);
      }
      if (ack.toInt() == check) {
        if (DEBUG)
          Serial.print(" Checksum OK ");
        stat = true;
      }
    }
  }
  return stat;
}

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

void sendMessage(String payload) {
  if (DEBUG) {
     Serial.print("Sending payload : ");
     Serial.print(payload);
  }
  
  // send packet
  LoRa.beginPacket();
  LoRa.print(payload);
  LoRa.endPacket();
}
