#ifndef PLANT_MQTT_H
#define PLANT_MQTT_H

#include <Arduino.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include "config.h"
#include <mbedtls/aes.h>
#include <mbedtls/gcm.h>
#include <mbedtls/md.h> 

class mqtt_handler {
public:
    mqtt_handler();
    bool begin();
    bool sendMessage(const String& message);
    bool registerDevice(const String& esp32Id, const String& plantName);
    bool isConnected();
    void loop();
    String getFormattedClientId();

private:
    WiFiClientSecure espClient;
    PubSubClient client;
    String apiKey;
    bool connect();
};

#endif // PLANT_MQTT_H