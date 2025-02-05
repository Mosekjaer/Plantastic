# Plant Monitoring & Management System

Welcome to the **Plant Monitoring & Management System** project! This repository contains all the components necessary for monitoring and controlling plants using IoT technology. It features a frontend client, a secure backend API using Go, an ESP32 module for sensor/motor interactions, and robust communication via MQTT.

---

## Architecture

This project is divided into the following key components:

1. **Client**:
   - A modern frontend built with [Vite](https://vitejs.dev/), providing a user-friendly interface for monitoring and controlling the system.
   
2. **API**:
   - Backend implemented in Go, which integrates with the frontend and handles Paseto token-based authentication for secure requests.
   
3. **ESP32**:
   - The IoT device responsible for collecting sensor data (e.g., soil moisture, temperature) and controlling actuators (e.g., water pumps). It sends data to the backend using MQTT via the [EMQX](https://www.emqx.io/) platform.

4. **Docs**:
   - Comprehensive documentation about the project, including setup instructions, architecture details, future goals, and more.

---

## Features

- **Real-time Plant Monitoring**: 
    - Track environmental data like temperature, humidity, and soil moisture in real-time via the frontend.
- **MQTT Communication**:
    - Seamless data transfer from the ESP32 module to the backend/api via the EMQX MQTT broker.
- **Secure Authentication**:
    - User authentication implemented with Paseto tokens for strong security.
- **Web Control Panel**:
    - Easy-to-use Vite-based frontend to review data and manage your plant system (like triggering watering).
- **ESP32 Integration**:
    - Robust IoT capabilities utilizing the ESP32 for real-world data collection and actuation.

---

## Technologies Used

### Frontend
- **Framework**: Vite
- **UI**: TailwindCSS

### Backend
- **Programming Language**: Go (Golang)
- **Authentication**: Paseto tokens
- **API Framework**: Gin

### IoT & Messaging
- **Hardware**: LILYGO® T-Higrow ESP32 Soil Tester DHT11 BEM280
- **Protocol**: MQTT
- **Broker**: EMQX (EMQX Cloud or Local)

### Documentation
- Coming
