using api.Interfaces;
using api.Models;

namespace api.Services
{
    public class SensorDataService : ISensorDataService
    {
        private readonly ISensorDataRepository _sensorDataRepository;
        private readonly IDeviceService _deviceService;
        private readonly ILogger<SensorDataService> _logger;
        private readonly GeminiService _geminiService;
        private readonly EmailService _emailService;
        private readonly IDeviceNotificationRepository _notificationRepository;
        private readonly IUserService _userService;

        public SensorDataService(
            ISensorDataRepository sensorDataRepository,
            IDeviceService deviceService,
            ILogger<SensorDataService> logger,
            GeminiService geminiService,
            EmailService emailService,
            IDeviceNotificationRepository notificationRepository,
            IUserService userService)
        {
            _sensorDataRepository = sensorDataRepository;
            _deviceService = deviceService;
            _logger = logger;
            _geminiService = geminiService;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
            _userService = userService;
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

        /// <summary>
        /// Validation of sensordata. This is also done in the esp32 itself, but a small server side check never hurts I guess.
        /// </summary>
        /// <param name="sensorData"></param>
        /// <returns>boolean whether sensor data is within a valid range</returns>
        private bool IsValidSensorData(SensorData sensorData)
        {
            if (sensorData.SoilMoisture < 0 || sensorData.SoilMoisture > 100)
            {
                _logger.LogWarning("Invalid soil moisture value: {SoilMoisture}", sensorData.SoilMoisture);
                return false;
            }

            if (sensorData.Temperature < -5 || sensorData.Temperature > 55)
            {
                _logger.LogWarning("Invalid temperature value: {Temperature}°C", sensorData.Temperature);
                return false;
            }

            if (sensorData.Humidity < 0 || sensorData.Humidity > 100)
            {
                _logger.LogWarning("Invalid humidity value: {Humidity}", sensorData.Humidity);
                return false;
            }

            if (sensorData.Light < 0 || sensorData.Light > 70000)
            {
                _logger.LogWarning("Invalid light value: {Light}", sensorData.Light);
                return false;
            }

            if (sensorData.Salt < 0 || sensorData.Salt > 5000)
            {
                _logger.LogWarning("Invalid salt value: {Salt}", sensorData.Salt);
                return false;
            }

            return true;
        }

        public async Task<bool> ProcessSensorDataAsync(string esp32Id, SensorData sensorData)
        {
            try
            {
                if (!IsValidSensorData(sensorData))
                {
                    _logger.LogWarning("Sensor data validation failed for device: {Esp32Id}", esp32Id);
                    return false;
                }

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

                sensorData.Esp32Id = esp32Id;
                sensorData.DeviceId = device.Id;

                await _sensorDataRepository.CreateAsync(sensorData);
                _logger.LogInformation("Sensor data saved for device: {DeviceName} (ESP32 ID: {Esp32Id})", device.Name, esp32Id);

                // Check if we should analyze with Gemini (24-hour cooldown)
                var lastNotification = await _notificationRepository.GetByDeviceIdAsync(device.Id);
                var shouldAnalyze = lastNotification == null || 
                    (DateTime.UtcNow - lastNotification.LastNotificationSent).TotalHours >= 24;

                if (shouldAnalyze)
                {
                    await AnalyzeAndNotifyAsync(sensorData, device);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor data for ESP32 ID: {Esp32Id}", esp32Id);
                return false;
            }
        }

        private async Task AnalyzeAndNotifyAsync(SensorData sensorData, Device device)
        {
            try
            {
                var analysis = await _geminiService.AnalyzePlantHealthAsync(sensorData, device);
                
                if (analysis.NeedsAttention)
                {
                    var userEmail = await _userService.GetUserEmailById(device.UserId);
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        _logger.LogError("Could not find email for user: {UserId}, device: {DeviceId}", device.UserId, device.Id);
                        return;
                    }

                    // Send email notification
                    await _emailService.SendPlantHealthEmailAsync(
                        userEmail,
                        device.Name,
                        analysis
                    );

                    var notification = await _notificationRepository.GetByDeviceIdAsync(device.Id);
                    if (notification == null)
                    {
                        notification = new DeviceNotification
                        {
                            DeviceId = device.Id,
                            LastNotificationSent = DateTime.UtcNow
                        };
                        await _notificationRepository.CreateAsync(notification);
                    }
                    else
                    {
                        notification.LastNotificationSent = DateTime.UtcNow;
                        await _notificationRepository.UpdateAsync(notification.Id!, notification);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing plant health data for device: {DeviceId}", device.Id);
            }
        }
    }
}
