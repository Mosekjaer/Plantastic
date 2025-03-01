#include "plant_webportal.h"
#include <nvs_flash.h>
#include <mqtt_handler.h>

WebPortal webPortal;

// HTML constants stored in PROGMEM
static const char PROGMEM HTML_HEAD[] = 
    "<!DOCTYPE html><html><head>"
    "<meta charset='UTF-8'>"
    "<meta name='viewport' content='width=device-width,initial-scale=1'>"
    "<style>"
    "body{font:14px Arial;margin:20px;background:#f0f0f0}"
    ".c{max-width:400px;margin:auto;background:#fff;padding:20px;border-radius:8px;box-shadow:0 2px 4px #0003}"
    "input,select{width:100%;padding:8px;margin:8px 0;box-sizing:border-box}"
    "button{background:#4CAF50;color:#fff;padding:10px;border:0;width:100%}"
    ".hint{color:#666;font-size:12px;margin:4px 0}"
    ".device-id{background:#f8f8f8;padding:10px;border-radius:4px;text-align:center;font-family:monospace;margin:10px 0}"
    "</style></head><body><div class='c'>"
    "<h2>Plant Monitor Setup</h2>"
    "<div class='device-id'>"
    "<strong>Your Device ID:</strong><br>%s"
    "<p class='hint'>You'll need this ID to claim your device in the web interface</p>"
    "</div>"
    "<form action='/save' method='post' id='setupForm'>";

static const char PROGMEM HTML_TAIL[] = 
    "<button type='submit'>Save</button></form></div>"
    "<script>"
    "document.getElementById('setupForm').addEventListener('submit',function(e){"
    "var b=this.querySelector('button');b.disabled=true;b.textContent='Saving...';"
    "});</script></body></html>";

static const char PROGMEM HTML_SAVED[] = 
    "<!DOCTYPE html><html><head>"
    "<meta charset='UTF-8'>"
    "<meta http-equiv='refresh' content='3;url=/'>"
    "<style>body{font:14px Arial;margin:20px;background:#f0f0f0}"
    ".c{max-width:400px;margin:auto;background:#fff;padding:20px;border-radius:8px;box-shadow:0 2px 4px #0003}"
    "</style></head>"
    "<body><div class='c'>"
    "<h2>Configuration Saved</h2>"
    "<p>Your settings have been saved. The device will now restart...</p>"
    "</div></body></html>";

WebPortal::WebPortal() : server(WEB_SERVER_PORT) {
    // Initialize preferences in constructor
    if (!preferences.begin("plantcare", false)) {
        #ifdef DEBUG_MODE
        Serial.println("Failed to initialize preferences");
        #endif
    }
}

void WebPortal::begin() {
    if (!isConfigured()) {
        // Set up DNS server first
        dnsServer.start(53, "*", WiFi.softAPIP());
        
        // Configure access point after
        WiFi.mode(WIFI_AP);
        WiFi.softAP(FPSTR(AP_SSID), FPSTR(AP_PASSWORD));
        
        // Set up routes for the server
        server.on("/", HTTP_GET, [this]() { handleRoot(); });
        server.on("/save", HTTP_POST, [this]() { handleSave(); });
        server.onNotFound([this]() { handleNotFound(); });
        server.begin();
        
        #ifdef DEBUG_MODE
        Serial.print(F("Connect to AP: "));
        Serial.println(FPSTR(AP_SSID));
        Serial.print(F("Password: "));
        Serial.println(FPSTR(AP_PASSWORD));
        Serial.print(F("Then visit: http://"));
        Serial.println(WiFi.softAPIP());
        #endif
    }
}

void WebPortal::handleRoot() {
    WiFi.scanDelete(); 
    
    #ifdef DEBUG_MODE
    Serial.println(F("Starting WiFi scan..."));
    #endif
    
    int n = WiFi.scanNetworks();
    
    String form;
    form.reserve(2048);
    
    uint64_t chipid = ESP.getEfuseMac();
    uint32_t chip = (uint32_t)(chipid >> 32);
    uint16_t chip1 = (uint16_t)(chipid);
    char deviceId[20];
    snprintf(deviceId, 20, "%08X%04X", chip, chip1);
    
    char htmlHead[strlen_P(HTML_HEAD) + 32];  
    sprintf_P(htmlHead, HTML_HEAD, deviceId);
    form += htmlHead;
    
    form += "<label>WiFi Network</label><select name='s' required>";
    if (n < 0) {
        form += F("<option value=''>Error scanning</option>");
    } else if (n == 0) {
        form += F("<option value=''>No networks found</option>");
    } else {
        struct Network {
            String ssid;
            int32_t rssi;
        };
        Network* networks = new Network[n];
        
        for (int i = 0; i < n; i++) {
            networks[i].ssid = WiFi.SSID(i);
            networks[i].rssi = WiFi.RSSI(i);
        }
        
        for (int i = 0; i < n - 1; i++) {
            for (int j = i + 1; j < n; j++) {
                if (networks[j].rssi > networks[i].rssi) {
                    Network temp = networks[i];
                    networks[i] = networks[j];
                    networks[j] = temp;
                }
            }
        }
        
        for (int i = 0; i < min(n, 10); i++) {
            form += "<option value='" + networks[i].ssid + "'>" + 
                   networks[i].ssid + " (" + networks[i].rssi + "dBm)</option>";
        }
        
        delete[] networks;
    }
    form += "</select>";
    
    form += F("<br><label>WiFi Password</label>"
              "<br><input type='password' name='p' required>"
              "<br><label>Plant Name</label>"
              "<br><input name='n' placeholder='e.g. Living Room Plant' required>"
              "<br><p class='hint'>Give your plant a unique name to identify it</p>");
    
    form += FPSTR(HTML_TAIL);
    
    server.sendHeader("Cache-Control", "no-cache, no-store, must-revalidate");
    server.sendHeader("Pragma", "no-cache");
    server.sendHeader("Expires", "-1");
    server.send(200, F("text/html"), form);
    
    WiFi.scanDelete();
}

void WebPortal::handleSave() {
    String ssid = server.arg("s");
    String pass = server.arg("p");
    String plantName = server.arg("n");
    
    uint64_t chipid = ESP.getEfuseMac();
    uint32_t chip = (uint32_t)(chipid >> 32);
    uint16_t chip1 = (uint16_t)(chipid);
    char deviceId[20];
    snprintf(deviceId, 20, "%08X%04X", chip, chip1);
    
    saveCredentials(ssid, pass, plantName);
    
    WiFi.begin(ssid.c_str(), pass.c_str());
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 20) {
        delay(500);
        attempts++;
    }
    
    if (WiFi.status() == WL_CONNECTED) {
        mqtt_handler mqtt;
        if (mqtt.registerDevice(deviceId, plantName)) {
            server.send(200, F("text/html"), FPSTR(HTML_SAVED));
            delay(1000);
            ESP.restart();
        } else {
            #ifdef DEBUG_MODE
            Serial.println("Registration failed, clearing preferences");
            #endif
            
            // Clear preferences
            preferences.end();
            delay(100);
            nvs_flash_erase();
            nvs_flash_init();
            preferences.begin("plantcare", false);
            preferences.clear();
            
            server.send(500, F("text/html"), F("Failed to register device. Please try again."));
            delay(1000);
            ESP.restart();
        }
    } else {
        server.send(500, F("text/html"), F("Failed to connect to WiFi. Please check credentials."));
    }
}

void WebPortal::handleNotFound() {
    server.sendHeader("Location", "/", true);
    server.send(302, F("text/plain"), "");
}

void WebPortal::saveCredentials(const String& ssid, const String& pass, const String& plantName) {
    #ifdef DEBUG_MODE
    Serial.println("Saving credentials:");
    Serial.print("Plant Name: "); Serial.println(plantName);
    #endif

    preferences.end();
    delay(100);

    nvs_flash_erase();
    nvs_flash_init();
    
    if (preferences.begin("plantcare", false)) {
        bool success = true;
        success &= preferences.putString(NVS_WIFI_SSID, ssid);
        success &= preferences.putString(NVS_WIFI_PASS, pass);
        success &= preferences.putString(NVS_PLANT_NAME, plantName);

        #ifdef DEBUG_MODE
        Serial.print("Save operations successful: "); Serial.println(success ? "Yes" : "No");
        Serial.println("Verifying saved values:");
        Serial.print("Saved Plant Name: "); Serial.println(preferences.getString(NVS_PLANT_NAME, ""));
        #endif
    } else {
        #ifdef DEBUG_MODE
        Serial.println("Failed to open preferences for writing!");
        #endif
    }
}

bool WebPortal::isConfigured() {
    return preferences.getString(NVS_WIFI_SSID, "").length() > 0;
}

String WebPortal::getWifiSSID() { 
    return preferences.getString(NVS_WIFI_SSID, "");
}

String WebPortal::getWifiPassword() { 
    return preferences.getString(NVS_WIFI_PASS, "");
}

String WebPortal::getPlantName() { 
    return preferences.getString(NVS_PLANT_NAME, "");
}

void WebPortal::setLastNotification(const String& message) {
    lastNotification = message;
}

void WebPortal::handleClient() {
    server.handleClient();
    dnsServer.processNextRequest();
}