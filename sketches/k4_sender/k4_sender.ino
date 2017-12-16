#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#include <SPI.h>
#include <LoRa.h>

#define DEBUG 1

#define OLED_RESET D0  // GPIO0
Adafruit_SSD1306 display(OLED_RESET);

const long freq = 868E6;
const int SF = 9;
const int retryMessageSending = 2;
//In Ms
const int waitForAck = 200;
const long bw = 125E3;

int counter = 1, messageLostCounter = 0;

void setup() {

  //Set Serial baudrate
  Serial.begin(115200);
  while (!Serial);

  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);  // initialize with the I2C addr 0x3C (for the 64x48)
  //display.display();  
  //display.clearDisplay();

  //Set LoRa Pins to work with Wemos
  LoRa.setPins(16, 17, 15); // set CS, reset, IRQ pin

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

void clearScreen() {
  // Efface l'écran et positionne le curseur dans le coin supérieur gauche - clear display and set cursor on the top left corner
  display.clearDisplay();
  display.setTextSize(1);
  display.setTextColor(WHITE);
  display.setCursor(0,0);
}

void loop() {
  
  clearScreen();

  display.println("Temp.");  
  display.println(" c");
  display.display();

  String message = "Hello " + String(counter);
  
  sendMessage(message);
  
  int nackCounter = 0;
  while (!receiveAck(message) && nackCounter <= retryMessageSending) {

    Serial.println(" refused ");
    Serial.print(nackCounter);
    //LoRa.sleep();
    //delay(1000);
    sendMessage(message);
    nackCounter++;
  }

  if (nackCounter >= retryMessageSending) {
    if (DEBUG) {
      Serial.println("");
      Serial.println("--------------- MESSAGE LOST ---------------------");
    }
    messageLostCounter++;
    //delay(100);
  } else {
    if (DEBUG)
      Serial.println("Acknowledged ");
  } 

  counter++;
}

bool receiveAck(String message) {
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
      for (int i = 0; i < message.length(); i++) {
        check += message[i];
        //   Serial.print(message[i]);
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
        Serial.print(" Checksum OK ");
        stat = true;
      }
    }
  }
  return stat;
}

void sendMessage(String message) {
  if (DEBUG) {
     Serial.print("Sending message : ");
     Serial.print(message);
  }
  
  // send packet
  LoRa.beginPacket();
  LoRa.print(message);
  LoRa.endPacket();
}
