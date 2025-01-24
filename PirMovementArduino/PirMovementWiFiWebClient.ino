#include <WiFiS3.h>
#include <MQTT.h>
#include "arduino_secrets.h"

#define pirPin 2
#define lightPin 4
#define soundAOUT A0

char ssid[] = SECRET_SSID;
char pass[] = SECRET_PASS;

int status = WL_IDLE_STATUS;

char server[] = ""; // Type your actual IPV4

WiFiClient client;
MQTTClient MQTTclient;

int lastPirVal = LOW;
int pirVal;

int lightStatus = LOW;
int soundStatus = LOW;

unsigned long previousMillis = 0;
const int interval = 300;
bool toggleTone = false;

// MQTT Connection
void connectMQTT() {
  Serial.print("checking wifi...");
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(1000);
  }

  Serial.print("\nconnecting...");
  while (!MQTTclient.connect("arduino", "public", "public")) {
    Serial.print(".");
    delay(100);
  }
  MQTTclient.subscribe("/sensor/arduino/pir");

  Serial.println("\nconnected!");
}

// MQTT Message Receiver
void messageReceived(String &topic, String &payload) {
  Serial.println("incoming: " + topic + " - " + payload);
  if (topic == "/sensor/arduino/pir") {
    if (payload == "Light1") {
      Serial.println("Light change to HIGH");
      digitalWrite(lightPin, HIGH);
    } else if (payload == "Light0") {
      Serial.println("Light change to LOW");
      digitalWrite(lightPin, LOW);
    } else if (payload == "Sound1") {
      Serial.println("Sound change to HIGH");
      soundStatus = HIGH;
      soundStart();
    } else if (payload == "Sound0") {
      Serial.println("Sound change to LOW");
      soundStatus = LOW;
      noTone(soundAOUT);
    }
  }
}

void soundStart() {
  if (soundStatus != LOW) {
    unsigned long currentMillis = millis();

    // Check if the interval has elapsed
    if (currentMillis - previousMillis >= interval) {
      previousMillis = currentMillis;  // Update the last time check

      // Toggle between the two tones
      if (toggleTone) {
        tone(soundAOUT, 440);
      } else {
        tone(soundAOUT, 600);
      }

      toggleTone = !toggleTone;  // Alternate the toggle
    }
  } else {
    // Stop sound if soundStatus is LOW
    noTone(soundAOUT);
  }
}

void setup() {
  pinMode(pirPin, INPUT);
  pinMode(lightPin, OUTPUT);
  pinMode(soundAOUT, OUTPUT);
  Serial.begin(115200);

  // check for the WiFi module:
  if (WiFi.status() == WL_NO_MODULE) {
    Serial.println("Communication with WiFi module failed!");
  }

  String fv = WiFi.firmwareVersion();
  if (fv < WIFI_FIRMWARE_LATEST_VERSION) {
    Serial.println("Please upgrade the firmware");
  }

  // attempt to connect to WiFi network:
  while (status != WL_CONNECTED) {
    Serial.print("Attempting to connect to SSID: ");
    Serial.println(ssid);
    status = WiFi.begin(ssid, pass);

    delay(1000);
  }

  printWifiStatus();

  Serial.println("\nStarting connection to server...");

  MQTTclient.begin("192.168.0.195", client);
  
  MQTTclient.onMessage(messageReceived);

  connectMQTT();
}

void loop() {
  MQTTclient.loop();

  if (!MQTTclient.connected()) {
    connectMQTT();
  }

  // Get pir sensor value
  pirVal = digitalRead(pirPin);

  Serial.println(pirVal);
  
  // Send message to MQTT Broker if pirVal is HIGH
  if (pirVal == HIGH) {
    MQTTclient.publish("/pir/movement", "1");
    Serial.println("publishing: /pir/movement 1");
    delay(2000);
  }

  delay(1000);
}

void printWifiStatus() {
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());

  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.print(rssi);
  Serial.println(" dBm");
}
