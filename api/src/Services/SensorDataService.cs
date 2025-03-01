using api.Interfaces;
using api.Models;

namespace api.Services
{
    public class SensorDataService : ISensorDataService
    {
        private readonly ISensorDataRepository _sensorDataRepository;
        private readonly ILogger<SensorDataService> _logger;

        public SensorDataService(ISensorDataRepository sensorDataRepository, ILogger<SensorDataService> logger)
        {
            _sensorDataRepository = sensorDataRepository;
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

        public async Task ProcessSensorDataAsync(string esp32Id, SensorData sensorData)
        {
            try
            {
                sensorData.Esp32Id = esp32Id;

                if (string.IsNullOrEmpty(sensorData.PlantName))
                {
                    _logger.LogWarning("Plant name is missing for ESP32 ID: {Esp32Id}", esp32Id);
                    sensorData.PlantName = "Unknown Plant"; 
                }

                await _sensorDataRepository.CreateAsync(sensorData);
                _logger.LogInformation("Sensor data saved for ESP32 ID: {Esp32Id}", esp32Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor data for ESP32 ID: {Esp32Id}", esp32Id);
            }
        }
    }
}
