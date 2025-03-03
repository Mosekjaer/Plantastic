using Microsoft.Extensions.Hosting;
using api.Models;
using api.Interfaces;

namespace api.Services
{
    public class PlantHealthAnalysisService : BackgroundService
    {
        private readonly IDeviceService _deviceService;
        private readonly IUserService _userService;
        private readonly ISensorDataService _sensorDataService;
        private readonly ILogger<PlantHealthAnalysisService> _logger;

        public PlantHealthAnalysisService(
            IDeviceService deviceService,
            IUserService userService,
            ISensorDataService sensorDataService,
            ILogger<PlantHealthAnalysisService> logger)
        {
            _deviceService = deviceService;
            _userService = userService;
            _sensorDataService = sensorDataService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAllUsersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing plant health analysis");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessAllUsersAsync()
        {
            var users = await _userService.GetAllUsersAsync();
            var utcNow = DateTime.UtcNow;

            foreach (var user in users)
            {
                try
                {
                    var userLocalTime = DateTime.UtcNow.AddMinutes(user.TimezoneOffsetMinutes);
                    var nextNotificationTime = userLocalTime.Date.Add(user.NotificationTime);
                    
                    if (userLocalTime.TimeOfDay >= user.NotificationTime)
                    {
                        nextNotificationTime = nextNotificationTime.AddDays(1);
                    }

                    var devices = await _deviceService.GetDevicesByUserIdAsync(user.Id);
                    foreach (var device in devices)
                    {
                        var analysisStartTime = DateTime.UtcNow.AddHours(-user.NotificationIntervalHours);
                        
                        await _sensorDataService.AnalyzeDeviceDataAsync(device.Id, analysisStartTime);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing analysis for user: {UserId}", user.Id);
                }
            }
        }
    }
} 