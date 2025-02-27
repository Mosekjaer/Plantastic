#ifndef PLANT_WEBPORTAL_H
#define PLANT_WEBPORTAL_H

#include <Arduino.h>
#include <WiFi.h>
#include <ESPmDNS.h>
#include <DNSServer.h>
#include <WebServer.h>
#include <Preferences.h>
#include "config.h"

class WebPortal {
public:
    WebPortal();
    void begin();
    void handleClient();
    bool isConfigured();
    String getWifiSSID();
    String getWifiPassword();
    String getPlantName();
    void setLastNotification(const String& message);

private:
    WebServer server;
    DNSServer dnsServer;
    Preferences preferences;
    String lastNotification;
    
    void handleRoot();
    void handleSave();
    void handleNotFound();
    void saveCredentials(const String& ssid, const String& pass, 
                        const String& plantName);
};

extern WebPortal webPortal;

#endif // PLANT_WEBPORTAL_H 