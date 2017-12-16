#include <SPI.h>
#include <LoRa.h>

#define DEBUG 1

const long freq = 868E6;
const int SF = 9;
const long bw = 125E3;

void setup() {

  Serial.begin(115200);
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

     Serial.println("Ready...");
  }

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

void loop() {
  
  int packetSize = LoRa.parsePacket();
  if (packetSize) {
    // received a packet
    String message = "";
    while (LoRa.available()) {
      message = message + ((char)LoRa.read());
    }

    //Ok send message acknownledgement
    sendAck(message);
  }
}


