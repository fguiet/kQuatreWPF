#include <Adafruit_SSD1306.h>

#define PIN_LED 15
#define PIN_TRIGGER 16
#define PIN_BUTTON 14
#define MAX_CHANNEL 16

#define OLED_RESET 4
Adafruit_SSD1306 display(OLED_RESET);

unsigned int channel = 0;
int lastState = LOW;

void setup() {
  pinMode(PIN_TRIGGER, INPUT);
  pinMode(PIN_LED, OUTPUT);
  pinMode (PIN_BUTTON, INPUT_PULLUP);
  
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);  // initialize with the I2C addr 0x3D (for the 128x64) 
  // init done
  
  updateDisplay();
}

void loop() {

  if (digitalRead(PIN_BUTTON) == LOW) {
    channel = 0;
    updateDisplay();
  }

  //if ((millis() - lastDebounceTime) > debounceDelay) {
  //  canTrigger = true;    
  //}

  if (digitalRead(PIN_TRIGGER) == LOW) {
    turnLed(false);
    lastState = LOW;
    delay(100); //debounce time :)
  }
  
  if (digitalRead(PIN_TRIGGER) == HIGH && lastState != HIGH) {
    lastState = HIGH;
    turnLed(true);    
    channel++;       
    updateDisplay();
  }  
}

void turnLed(bool on) {
  if (on) {
    digitalWrite(PIN_LED, HIGH);
  }
  else {
    digitalWrite(PIN_LED, LOW);
  }
}

void updateDisplay() {
  display.clearDisplay();
  display.setTextColor(WHITE);  
  display.setCursor(40, 0);
  display.print("k4 Tester");
  display.setCursor(25, 15);
  display.print("Canal : " );       
  display.print(channel);    
  display.print(" / " );   
  display.print(MAX_CHANNEL);   
  display.display();      
}
