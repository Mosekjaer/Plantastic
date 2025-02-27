#include "mqtt_handler.h"
#include <Preferences.h>
#include "config.h"
#include <PubSubClient.h>
#include <WiFiClientSecure.h>
#include <mbedtls/md.h>  // For SHA-256

mqtt_handler::mqtt_handler() : client(espClient) {
    espClient.setCACert(MQTT_CERT);
}

bool mqtt_handler::begin() {
    client.setServer(MQTT_HOST, MQTT_PORT);
    return connect();
}

bool mqtt_handler::connect() {
    char clientId[32];
    snprintf(clientId, sizeof(clientId), MQTT_CLIENT_ID, random(0xffff));
    
    #ifdef DEBUG_MODE
    Serial.print("Attempting MQTT connection with client ID: ");
    Serial.println(clientId);
    Serial.print("MQTT Host: ");
    Serial.println(MQTT_HOST);
    Serial.print("MQTT Port: ");
    Serial.println(MQTT_PORT);
    #endif

    if (client.connect(clientId, MQTT_USERNAME, MQTT_PASSWORD)) {
        #ifdef DEBUG_MODE
        Serial.println("Connected to MQTT broker");
        #endif
        return true;
    } else {
        #ifdef DEBUG_MODE
        Serial.print("MQTT connection failed, rc=");
        Serial.println(client.state());
        // Print meaning of return code
        switch(client.state()) {
            case -4: Serial.println("MQTT_CONNECTION_TIMEOUT"); break;
            case -3: Serial.println("MQTT_CONNECTION_LOST"); break;
            case -2: Serial.println("MQTT_CONNECT_FAILED"); break;
            case -1: Serial.println("MQTT_DISCONNECTED"); break;
            case 1: Serial.println("MQTT_CONNECT_BAD_PROTOCOL"); break;
            case 2: Serial.println("MQTT_CONNECT_BAD_CLIENT_ID"); break;
            case 3: Serial.println("MQTT_CONNECT_UNAVAILABLE"); break;
            case 4: Serial.println("MQTT_CONNECT_BAD_CREDENTIALS"); break;
            case 5: Serial.println("MQTT_CONNECT_UNAUTHORIZED"); break;
        }
        #endif
        return false;
    }
}

std::string getUniqueId() {
    uint64_t chipid = ESP.getEfuseMac();
    uint32_t chip = (uint32_t)(chipid >> 32);
    uint16_t chip1 = (uint16_t)(chipid);
  
    char id_string[20];
    snprintf(id_string, 20, "%08X%04X", chip, chip1);
    return std::string(id_string);
}

bool mqtt_handler::sendMessage(const String& message) {
    if (!client.connected() && !connect()) {
        #ifdef DEBUG_MODE
        Serial.println("Not connected to MQTT broker and reconnection failed");
        #endif
        return false;
    }

    std::string unique_device_id = getUniqueId();
    #ifdef DEBUG_MODE
    Serial.print("Unique device ID: ");
    Serial.println(unique_device_id.c_str());
    #endif

    // Generate topic: unique_name/status
    char topic[256];
    snprintf(topic, sizeof(topic), MQTT_TOPIC_STATUS, 
             unique_device_id.c_str());
    
    #ifdef DEBUG_MODE
    Serial.print("Publishing to topic: ");
    Serial.println(topic);
    Serial.print("Topic length: ");
    Serial.println(strlen(topic));
    Serial.print("Message length: ");
    Serial.println(message.length());
    
    // Try publishing to a test topic first
    Serial.println("Trying test publish...");
    if (!client.publish("test/status", "test")) {
        Serial.println("Test publish failed");
        Serial.print("MQTT state: ");
        Serial.println(client.state());
    } else {
        Serial.println("Test publish succeeded");
    }
    #endif

    bool result = client.publish(topic, message.c_str());
    
    #ifdef DEBUG_MODE
    if (!result) {
        Serial.println("Publish failed");
        Serial.print("MQTT state after publish attempt: ");
        Serial.println(client.state());
    } else {
        Serial.println("Publish successful");
    }
    #endif

    return result;
}

bool mqtt_handler::isConnected() {
    return client.connected();
}

void mqtt_handler::loop() {
    if (!client.connected()) {
        connect();
    }
    client.loop();
}

String mqtt_handler::getFormattedClientId() {
    char clientId[32];
    snprintf(clientId, sizeof(clientId), MQTT_CLIENT_ID, (uint16_t)(ESP.getEfuseMac() >> 32));
    return String(clientId);
}