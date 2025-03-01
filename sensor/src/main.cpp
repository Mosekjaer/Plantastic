#include <Arduino.h>
#include <DHT.h>
#include <Wire.h>
#include <BH1750.h>
#include <WiFi.h>
#include <WebServer.h>
#include <ESPmDNS.h>
#include <HTTPClient.h>
#include <Esp.h>
#include <ArduinoJson.h>
#include <Preferences.h>
#include <time.h>
#include "config.h"
#include "plant_webportal.h"
#include "mqtt_handler.h"

// Store constant strings in flash memory
static const char PROGMEM STR_PLANT_MONITOR[] = "Plant Monitor Starting...";
static const char PROGMEM STR_BOOT_COUNT[] = "Boot count: %d\n";
static const char PROGMEM STR_NO_CONFIG[] = "No configuration found. Entering config mode...";

BH1750 lightMeter(0x23);
DHT dht(DHT_PIN, DHT_TYPE);
Preferences preferences;
mqtt_handler mqtt;

RTC_DATA_ATTR int bootCount = 0;
unsigned long configStartTime = 0;

bool initializeSensors();
void setupConfigMode();
uint16_t readSoil();
float readBattery();
uint32_t readSalt();
void checkPlantStatus();
bool connectWiFi();

void print_wakeup_reason() {
    #ifdef DEBUG_MODE
    esp_sleep_wakeup_cause_t wakeup_reason = esp_sleep_get_wakeup_cause();
    Serial.print(F("Wakeup caused by: "));
    switch(wakeup_reason) {
        case ESP_SLEEP_WAKEUP_TIMER: Serial.println(F("timer")); break;
        case ESP_SLEEP_WAKEUP_EXT0: Serial.println(F("external signal using RTC_IO")); break;
        case ESP_SLEEP_WAKEUP_EXT1: Serial.println(F("external signal using RTC_CNTL")); break;
        case ESP_SLEEP_WAKEUP_TOUCHPAD: Serial.println(F("touchpad")); break;
        case ESP_SLEEP_WAKEUP_ULP: Serial.println(F("ULP program")); break;
        default: Serial.println(F("other")); break;
    }
    #endif
}

void goToSleep() {
    digitalWrite(POWER_CTRL, 0);
    esp_sleep_enable_timer_wakeup(SLEEP_DURATION * uS_TO_S_FACTOR);
    #ifdef DEBUG_MODE
    Serial.flush();
    #endif
    esp_deep_sleep_start();
}

void setup() {
    Serial.begin(115200);
    
    // Add button check for reset
    pinMode(USER_BUTTON, INPUT);
    if (digitalRead(USER_BUTTON) == LOW) {
        Serial.println(F("Reset button pressed, clearing configuration..."));
        preferences.begin("plantcare", false);
        preferences.clear();
        preferences.end();
        Serial.println(F("Configuration cleared. Restarting..."));
        delay(1000);
        ESP.restart();
    }
    
    Serial.printf("Boot count: %d\n", ++bootCount);
    print_wakeup_reason();
    
    preferences.begin("plantcare", false);
    
    // Power up sensors
    // Added a delay after power up to make sure the sensors power up in time.
    Serial.println("Powering up sensors...");
    pinMode(POWER_CTRL, OUTPUT);
    digitalWrite(POWER_CTRL, 1);
    delay(1000); 
    
    // Initialize sensors before checking configuration
    Serial.println("Initializing sensors...");
    initializeSensors();
    
    // If not configured, enter config mode
    if (!preferences.getString(NVS_WIFI_SSID, "").length()) {
        Serial.println("No configuration found. Entering config mode...");
        configStartTime = millis();
        setupConfigMode();
        return;
    }
    
    // Connect to WiFi and check plant
    if (connectWiFi()) {
        checkPlantStatus();
    }
    
    // Go to sleep after everything is done
    goToSleep();
}

void loop() {
    // Only used in config mode
    if (!preferences.getString(NVS_WIFI_SSID, "").length()) {
        webPortal.handleClient();
        
        // Check if config mode timeout has been reached
        if (millis() - configStartTime > CONFIG_MODE_TIMEOUT * 1000) {
            Serial.println("Configuration mode timeout reached. Going to sleep...");
            goToSleep();
        }
        delay(10);
    }
}

bool initializeSensors() {
    bool success = true;
    
    // Initialize DHT first
    dht.begin();
    
    // Initialize I2C and verify success
    Wire.begin(I2C_SDA, I2C_SCL);
    
    // Add I2C scanner to verify BH1750 is detected
    #ifdef DEBUG_MODE
    Serial.println("Scanning I2C bus...");
    byte error, address;
    int nDevices = 0;
    for(address = 1; address < 127; address++) {
        Wire.beginTransmission(address);
        error = Wire.endTransmission();
        if (error == 0) {
            Serial.print("I2C device found at address 0x");
            if (address < 16) Serial.print("0");
            Serial.println(address, HEX);
            nDevices++;
        }
    }
    if (nDevices == 0) {
        Serial.println("No I2C devices found!");
        success = false;
    }
    #endif
    
    // Try reinitializing BH1750 with explicit power on
    lightMeter.begin(BH1750::CONTINUOUS_HIGH_RES_MODE);
    delay(150); // Add delay after mode change
    
    // Test read to verify sensor
    float testRead = lightMeter.readLightLevel();
    #ifdef DEBUG_MODE
    Serial.print("Initial light reading: ");
    Serial.println(testRead);
    #endif
    
    if (testRead < 0) {
        Serial.println(F("Error: BH1750 not responding"));
        success = false;
    }
    
    return success;
}

void setupConfigMode() {
    Serial.println("Starting configuration portal...");
    WiFi.mode(WIFI_AP);
    WiFi.softAP(AP_SSID, AP_PASSWORD);
    
    Serial.printf("Connect to AP: %s\n", AP_SSID);
    Serial.printf("Password: %s\n", AP_PASSWORD);
    Serial.printf("Then visit: http://%s\n", WiFi.softAPIP().toString().c_str());
    
    webPortal.begin();
}

bool connectWiFi() {
    Serial.println("Connecting to WiFi...");
    String ssid = preferences.getString(NVS_WIFI_SSID, "");
    String pass = preferences.getString(NVS_WIFI_PASS, "");
    
    WiFi.begin(ssid.c_str(), pass.c_str());
    
    unsigned long startAttemptTime = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - startAttemptTime < WIFI_TIMEOUT) {
        Serial.print(".");
        delay(100);
    }
    Serial.println();
    
    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("✓ Connected to WiFi! IP: %s\n", WiFi.localIP().toString().c_str());
        
        // Then configure time servers
        configTime(0, 0, "pool.ntp.org", "time.nist.gov");
        
        // Wait for time to be set
        time_t now = time(nullptr);
        while (now < 24 * 3600) {
            delay(100);
            now = time(nullptr);
        }
        
        return true;
    } else {
        Serial.println("✗ Failed to connect to WiFi");
        return false;
    }
}

void checkPlantStatus() {
    // Add debug for light reading
    #ifdef DEBUG_MODE
    Serial.println("Reading light level...");
    #endif
    float luxRead = lightMeter.readLightLevel();
    if (luxRead < 0) {
        Serial.println("Error reading light sensor!");
        luxRead = 0;
    }
    #ifdef DEBUG_MODE
    Serial.print("Light level raw: ");
    Serial.println(luxRead);
    #endif
    
    uint16_t soil = readSoil();
    uint32_t salt = readSalt();
    float t = dht.readTemperature();
    float h = dht.readHumidity();
    float batt = readBattery();
    
    // Create JSON document for MQTT message
    StaticJsonDocument<512> doc;
    doc["light"] = luxRead;
    doc["soil_moisture"] = soil;
    doc["salt"] = salt;
    doc["temperature"] = t;
    doc["humidity"] = h;
    doc["battery"] = batt;
    
    // Get timestamp
    time_t now;
    time(&now);
    doc["timestamp"] = now;
    
    // Serialize JSON to string
    String message;
    serializeJson(doc, message);
    
    #ifdef DEBUG_MODE
    Serial.println(F("\nPlant Status:"));
    Serial.print(F("Message: "));
    Serial.println(message);
    #endif
    
    if (mqtt.begin()) {
        if (mqtt.sendMessage(message)) {
            #ifdef DEBUG_MODE
            Serial.println(F("MQTT message sent successfully"));
            #endif
            webPortal.setLastNotification(message);
        } else {
            #ifdef DEBUG_MODE
            Serial.println(F("Failed to send MQTT message"));
            #endif
        }
    } else {
        #ifdef DEBUG_MODE
        Serial.println(F("Failed to connect to MQTT broker"));
        #endif
    }
}

uint16_t readSoil() {
    uint16_t raw = analogRead(SOIL_PIN);
    uint16_t mapped = map(raw, SOIL_MIN, SOIL_MAX, 0, 100);
    return mapped;
}

float readBattery() {
    int vref = 1100;
    uint16_t volt = analogRead(BAT_ADC);
    float battery_voltage = ((float)volt / 4095.0) * 2.0 * 3.3 * (vref) / 1000;
    float percentage = map(battery_voltage * 100, 416, 290, 100, 0);
    return percentage;
}

uint32_t readSalt() {
    uint8_t samples = 120;
    uint32_t humi = 0;
    uint16_t array[120];
    
    for (int i = 0; i < samples; i++) {
        array[i] = analogRead(SALT_PIN);
        delay(2);
    }
    std::sort(array, array + samples);
    for (int i = 0; i < samples; i++) {
        if (i == 0 || i == samples - 1) continue;
        humi += array[i];
    }
    humi /= samples - 2;
    return humi;
}
