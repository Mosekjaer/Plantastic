#include "plant_webportal.h"
#include <nvs_flash.h>

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
    "</style></head><body><div class='c'><h2>Plant Monitor Setup</h2>"
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
    preferences.begin("plantcare", false);
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
    WiFi.scanDelete();  // Clear previous scan results
    
    // Start WiFi scan synchronously
    #ifdef DEBUG_MODE
    Serial.println(F("Starting WiFi scan..."));
    #endif
    
    int n = WiFi.scanNetworks();
    
    String form;
    form.reserve(2048);
    
    form += FPSTR(HTML_HEAD);
    
    // Add WiFi networks dropdown selector
    form += "<label>WiFi Network</label><select name='s' required>";
    if (n < 0) {
        form += F("<option value=''>Error scanning</option>");
    } else if (n == 0) {
        form += F("<option value=''>No networks found</option>");
    } else {
        // Sort networks by signal strength
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
    
    // Add other form fields with labels and hints
    form += F("<label>WiFi Password</label>"
              "<input type='password' name='p' required>"
              "<label>Plant Name</label>"
              "<input name='n' placeholder='e.g. Living Room Plant' required>"
              "<p class='hint'>Give your plant a unique name to identify it</p>");
    
    form += FPSTR(HTML_TAIL);
    
    server.sendHeader("Cache-Control", "no-cache, no-store, must-revalidate");
    server.sendHeader("Pragma", "no-cache");
    server.sendHeader("Expires", "-1");
    server.send(200, F("text/html"), form);
    
    WiFi.scanDelete();
}

void WebPortal::handleSave() {
    saveCredentials(
        server.arg("s"),  // SSID
        server.arg("p"),  // Password
        server.arg("n")
    );
    
    server.send(200, F("text/html"), FPSTR(HTML_SAVED));
    delay(1000);
    ESP.restart();
}

void WebPortal::handleNotFound() {
    server.sendHeader("Location", "/", true);
    server.send(302, F("text/plain"), "");
}

void WebPortal::saveCredentials(const String& ssid, const String& pass, 
                              const String& plantName) {
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

        preferences.end();
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
    if (!preferences.begin("plantcare", true)) {
        return "";
    }
    String value = preferences.getString(NVS_WIFI_SSID, "");
    preferences.end();
    return value;
}

String WebPortal::getWifiPassword() { 
    if (!preferences.begin("plantcare", true)) {
        return "";
    }
    String value = preferences.getString(NVS_WIFI_PASS, "");
    preferences.end();
    return value;
}

String WebPortal::getPlantName() { 
    if (!preferences.begin("plantcare", true)) {
        #ifdef DEBUG_MODE
        Serial.println("Failed to open preferences for reading plant name!");
        #endif
        return "";
    }
    String name = preferences.getString(NVS_PLANT_NAME, "");
    #ifdef DEBUG_MODE
    Serial.print("Retrieved Plant Name: "); Serial.println(name);
    Serial.print("Name length: "); Serial.println(name.length());
    #endif
    preferences.end();
    return name;
}


void WebPortal::setLastNotification(const String& message) {
    lastNotification = message;
}

void WebPortal::handleClient() {
    server.handleClient();
    dnsServer.processNextRequest();
}