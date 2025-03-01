using api.Interfaces;
using api.Models;

namespace api.Services
{
    public class SensorDataService : ISensorDataService
    {
        private readonly ISensorDataRepository _sensorDataRepository;
        private readonly IDeviceService _deviceService;
        private readonly ILogger<SensorDataService> _logger;

        public SensorDataService(
            ISensorDataRepository sensorDataRepository,
            IDeviceService deviceService,
            ILogger<SensorDataService> logger)
        {
            _sensorDataRepository = sensorDataRepository;
            _deviceService = deviceService;
            _logger = logger;
        }

        public async Task<List<SensorData>> GetAllSensorDataAsync()
        {
            try
            {
                return await _sensorDataRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensor data");
                return new List<SensorData>();
            }
        }

        public async Task<bool> ProcessSensorDataAsync(string esp32Id, SensorData sensorData)
        {
            try
            {
                // Validate the device exists and is active
                if (!await _deviceService.ValidateDeviceAsync(esp32Id))
                {
                    _logger.LogWarning("Attempt to process data for unregistered or inactive device: {Esp32Id}", esp32Id);
                    return false;
                }

                var device = await _deviceService.GetDeviceByEsp32IdAsync(esp32Id);
                if (device == null)
                {
                    _logger.LogError("Device not found after validation: {Esp32Id}", esp32Id);
                    return false;
                }

                // Set device and user information
                sensorData.Esp32Id = esp32Id;
                sensorData.DeviceId = device.Id;

                if (string.IsNullOrEmpty(sensorData.PlantName))
                {
                    sensorData.PlantName = device.Name;
                }

                await _sensorDataRepository.CreateAsync(sensorData);
                _logger.LogInformation("Sensor data saved for device: {DeviceName} (ESP32 ID: {Esp32Id})", device.Name, esp32Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor data for ESP32 ID: {Esp32Id}", esp32Id);
                return false;
            }
        }
    }
}
