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
    #ifdef DEBUG_MODE
    Serial.println("Setting up sensor pins...");
    #endif
    
    pinMode(POWER_CTRL, OUTPUT);
    pinMode(DHT_PIN, INPUT);
    pinMode(SALT_PIN, INPUT);
    pinMode(SOIL_PIN, INPUT);
    
    #ifdef DEBUG_MODE
    Serial.println("Powering up sensors...");
    Serial.printf("POWER_CTRL pin: %d\n", POWER_CTRL);
    Serial.printf("DHT_PIN: %d\n", DHT_PIN);
    Serial.printf("SALT_PIN: %d\n", SALT_PIN);
    Serial.printf("SOIL_PIN: %d\n", SOIL_PIN);
    #endif
    
    digitalWrite(POWER_CTRL, 1);
    
    #ifdef DEBUG_MODE
    Serial.println("Waiting for sensors to stabilize...");
    #endif
    delay(5000); 
    
    // Initialize sensors before checking configuration
    Serial.println("Initializing sensors...");
    if (!initializeSensors()) {
        Serial.println("Warning: Some sensors failed to initialize properly");
    }
    
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
    
    // Test analog pins first
    #ifdef DEBUG_MODE
    Serial.println("\nTesting analog pins...");
    Serial.printf("SALT_PIN (GPIO%d) raw value: %d\n", SALT_PIN, analogRead(SALT_PIN));
    Serial.printf("SOIL_PIN (GPIO%d) raw value: %d\n", SOIL_PIN, analogRead(SOIL_PIN));
    Serial.printf("BAT_ADC (GPIO%d) raw value: %d\n", BAT_ADC, analogRead(BAT_ADC));
    Serial.println("ADC readings above should be non-zero if pins are working\n");
    #endif
    
    // Initialize DHT with multiple attempts
    #ifdef DEBUG_MODE
    Serial.println("Initializing DHT sensor...");
    #endif
    
    dht.begin();
    delay(2000);
    
    // Test DHT readings with multiple attempts
    bool dht_success = false;
    for (int attempt = 0; attempt < 3 && !dht_success; attempt++) {
        #ifdef DEBUG_MODE
        Serial.printf("DHT read attempt %d...\n", attempt + 1);
        #endif
        
        float test_temp = dht.readTemperature();
        float test_hum = dht.readHumidity();
        
        #ifdef DEBUG_MODE
        Serial.print("DHT readings - Temp: ");
        Serial.print(test_temp);
        Serial.print("°C, Humidity: ");
        Serial.print(test_hum);
        Serial.println("%");
        #endif
        
        if (isnan(test_temp) || isnan(test_hum) || (test_temp == 0 && test_hum == 0)) {
            #ifdef DEBUG_MODE
            Serial.println("Invalid readings from DHT sensor, retrying...");
            #endif
            delay(2000); 
        } else {
            dht_success = true;
            #ifdef DEBUG_MODE
            Serial.println("DHT sensor initialized successfully!");
            #endif
        }
    }
    
    if (!dht_success) {
        #ifdef DEBUG_MODE
        Serial.println("WARNING: Failed to get valid readings from DHT sensor after multiple attempts!");
        Serial.println("The system will continue, but temperature and humidity readings may be incorrect.");
        #endif
        success = false;
    }

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
    bool light_success = false;
    for (int attempt = 0; attempt < 3 && !light_success; attempt++) {
        #ifdef DEBUG_MODE
        Serial.printf("Light sensor init attempt %d...\n", attempt + 1);
        #endif
        
        lightMeter.begin(BH1750::CONTINUOUS_HIGH_RES_MODE);
        delay(1000);
        
        // Test read to verify sensor
        float testRead = lightMeter.readLightLevel();
        #ifdef DEBUG_MODE
        Serial.print("Light reading: ");
        Serial.println(testRead);
        #endif
        
        if (testRead < 0) {
            #ifdef DEBUG_MODE
            Serial.println(F("Error: BH1750 not responding, retrying..."));
            #endif
            delay(1000);
        } else {
            light_success = true;
            #ifdef DEBUG_MODE
            Serial.println("Light sensor initialized successfully!");
            #endif
        }
    }
    
    if (!light_success) {
        #ifdef DEBUG_MODE
        Serial.println("WARNING: Failed to initialize light sensor after multiple attempts!");
        #endif
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

bool isValidSensorData(float light, uint16_t soil, uint32_t salt, float temp, float humidity, float battery) {
    bool valid = true;
    
    #ifdef DEBUG_MODE
    Serial.println(F("\nValidating sensor data:"));
    #endif
    
    if (isnan(temp) || isnan(humidity)) {
        #ifdef DEBUG_MODE
        Serial.println(F("Failed to read from DHT sensor! Values are NaN"));
        #endif
        valid = false;
    }

    // Check for zero values in temperature and humidity
    if (temp == 0 && humidity == 0) {
        #ifdef DEBUG_MODE
        Serial.println(F("Warning: Both temperature and humidity are zero - likely DHT sensor issue"));
        #endif
        // Don't invalidate the data - allow other sensors to work
        // valid = false;
    }

    if (temp < -10 || temp > 50) {
        #ifdef DEBUG_MODE
        Serial.printf("Invalid temperature reading: %.1f°C\n", temp);
        #endif
        valid = false;
    }

    if (humidity < 0 || humidity > 100) {
        #ifdef DEBUG_MODE
        Serial.printf("Invalid humidity reading: %.1f%%\n", humidity);
        #endif
        valid = false;
    }

    if (light < 0 || light > 65535) {
        #ifdef DEBUG_MODE
        Serial.printf("Invalid light reading: %.1f lux\n", light);
        #endif
        valid = false;
    }

    if (soil < 0 || soil > 100) {
        #ifdef DEBUG_MODE
        Serial.printf("Invalid soil moisture reading: %d%%\n", soil);
        #endif
        valid = false;
    }

    if (salt > 1000) { 
        #ifdef DEBUG_MODE
        Serial.printf("Invalid salt reading: %d\n", salt);
        #endif
        valid = false;
    }

    return valid;
}

void checkPlantStatus() {
    float luxRead = 0;
    uint16_t soil = 0;
    uint32_t salt = 0;
    float t = 0;
    float h = 0;
    float batt = 0;
    bool validData = false;
    bool dht_working = false;
    bool light_working = false;
    bool soil_working = false;
    bool salt_working = false;

    for (int i = 0; i < 5 && !validData; ++i) {
        // Add debug for light reading
        #ifdef DEBUG_MODE
        Serial.println("Reading light level...");
        #endif
        
        float temp_lux = lightMeter.readLightLevel();
        if (temp_lux < 0) {
            #ifdef DEBUG_MODE
            Serial.println("Error reading light sensor!");
            #endif
            light_working = false;
        } else {
            luxRead = temp_lux;
            light_working = true;
        }
        
        #ifdef DEBUG_MODE
        Serial.print("Light level raw: ");
        Serial.println(luxRead);
        #endif
        
        // Read soil moisture
        uint16_t temp_soil = readSoil();
        if (temp_soil > 0 && temp_soil <= 100) {
            soil = temp_soil;
            soil_working = true;
        }
        
        // Read salt level
        uint32_t temp_salt = readSalt();
        if (temp_salt > 0 && temp_salt < 1000) {
            salt = temp_salt;
            salt_working = true;
        }
        
        // Read temperature and humidity
        float temp_t = dht.readTemperature();
        float temp_h = dht.readHumidity();
        
        if (!isnan(temp_t) && !isnan(temp_h) && !(temp_t == 0 && temp_h == 0)) {
            t = temp_t;
            h = temp_h;
            dht_working = true;
        }
        
        batt = readBattery();

        #ifdef DEBUG_MODE
        Serial.println(F("\nRaw Sensor Readings:"));
        Serial.printf("Light: %.1f lux (working: %s)\n", luxRead, light_working ? "yes" : "no");
        Serial.printf("Soil Moisture: %d%% (working: %s)\n", soil, soil_working ? "yes" : "no");
        Serial.printf("Salt: %d (working: %s)\n", salt, salt_working ? "yes" : "no");
        Serial.printf("Temperature: %.1f°C (working: %s)\n", t, dht_working ? "yes" : "no");
        Serial.printf("Humidity: %.1f%% (working: %s)\n", h, dht_working ? "yes" : "no");
        Serial.printf("Battery: %.1f%%\n", batt);
        #endif

        // Consider data valid if at least some sensors are working
        if (soil_working || salt_working || dht_working || light_working) {
            validData = true;
            #ifdef DEBUG_MODE
            Serial.println(F("At least some sensors are working, proceeding with available data."));
            #endif
        } else {
            #ifdef DEBUG_MODE
            Serial.println(F("No sensors are working! Retrying..."));
            #endif
            delay(1000);
        }
    }

    if (!validData) {
        #ifdef DEBUG_MODE
        Serial.println(F("Failed to get valid sensor readings after multiple attempts. Skipping update."));
        #endif
        return;
    }
    
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
    
    #ifdef DEBUG_MODE
    Serial.println("Reading salt sensor values:");
    #endif
    
    for (int i = 0; i < samples; i++) {
        array[i] = analogRead(SALT_PIN);
        #ifdef DEBUG_MODE
        if (i < 5) {  
            Serial.print("Sample ");
            Serial.print(i);
            Serial.print(": ");
            Serial.println(array[i]);
        }
        #endif
        delay(2); 
    }
    std::sort(array, array + samples);
    
    // Check if all readings are zero
    bool all_zero = true;
    for (int i = 0; i < samples; i++) {
        if (array[i] != 0) {
            all_zero = false;
            break;
        }
    }
    
    #ifdef DEBUG_MODE
    if (all_zero) {
        Serial.println("Warning: All salt sensor readings are zero!");
    }
    #endif
    
    for (int i = 0; i < samples; i++) {
        if (i == 0 || i == samples - 1) continue;
        humi += array[i];
    }
    humi /= samples - 2;
    
    #ifdef DEBUG_MODE
    Serial.print("Final salt value: ");
    Serial.println(humi);
    if (humi < 201) {
        Serial.println("Salt level: NEEDED");
    } else if (humi < 251) {
        Serial.println("Salt level: LOW");
    } else if (humi < 351) {
        Serial.println("Salt level: OPTIMAL");
    } else {
        Serial.println("Salt level: TOO HIGH");
    }
    #endif
    
    return humi;
}
