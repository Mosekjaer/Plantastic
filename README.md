# Plant Monitoring & Management System (Plantastic)

A modern IoT-based plant monitoring and management system that combines hardware sensors, cloud infrastructure, and a user-friendly interface to help you take better care of your plants.

## 🌟 Project Overview

This project consists of three main components working together to provide a complete plant care solution:

### 🎯 Core Components

1. **Backend API** (`/api`)
   - Built with .NET 9 (Preview)
   - Secure JWT authentication
   - MongoDB for data storage
   - MQTT integration for real-time sensor data
   - Docker support for easy deployment
   - Gemini AI integration for plant care recommendations

2. **IoT Sensor Module** (`/sensor`)
   - ESP32-based hardware implementation
   - PlatformIO development environment
   - Real-time sensor monitoring:
     - Soil moisture
     - Temperature
     - Battery level
     - Salt level
   - Configurable deep sleep for power efficiency
   - Web portal for easy WiFi configuration
   - Secure MQTT communication

3. **Web Client** (`/client`)
   - *Coming soon*
   - Will provide a modern, responsive interface for plant monitoring

## 🛠️ Technology Stack

### Backend (API)
- **.NET 9.0** (Preview) with ASP.NET Core
- **Authentication**: JWT tokens
- **Database**: MongoDB
- **Messaging**: MQTT with EMQX broker
- **Logging**: Serilog with console and file sinks
- **AI Integration**: Google Gemini API
- **Containerization**: Docker & Docker Compose
- **Documentation**: OpenAPI/Swagger

### IoT Module
- **Hardware**: LILYGO® T-Higrow ESP32 Soil Tester DHT11 BEM280
- **Framework**: PlatformIO
- **Language**: C++
- **Communication**: MQTT over TLS
- **Configuration**: Web Portal for setup
- **Power Management**: Deep Sleep support

### CI/CD
- GitHub Actions workflows for both API and Sensor components
- Automated builds and tests
- Environment-specific configurations

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK (Preview)
- Docker & Docker Compose
- PlatformIO CLI
- MongoDB
- MQTT Broker (e.g., EMQX)

### Configuration Files
- API: Copy `.env.example` to `.env` and adjust settings
- Sensor: Copy `config.h.example` to `config.h` and set your configurations

### Running the Project

#### API
```bash
cd api
cp .env.example .env
# Edit .env with your settings
docker-compose up -d
```

#### Sensor
```bash
cd sensor
cp include/config.h.example include/config.h
# Edit config.h with your settings
pio run -t upload
```

## 🔒 Security

- Secure MQTT communication over TLS
- JWT-based API authentication
- Environment-based configuration
- Secrets management in CI/CD

## 📝 Documentation

Detailed documentation is available in the `/docs` directory:
- *Coming soon*

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests.

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.
