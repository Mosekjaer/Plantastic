using api.Interfaces;
using api.Models;

namespace api.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(IDeviceRepository deviceRepository, ILogger<DeviceService> logger)
        {
            _deviceRepository = deviceRepository;
            _logger = logger;
        }

        public async Task<Device> RegisterDeviceAsync(Device device)
        {
            try
            {
                var existingDevice = await _deviceRepository.GetByEsp32IdAsync(device.Esp32Id);
                if (existingDevice != null)
                {
                    throw new InvalidOperationException($"Device with ESP32 ID {device.Esp32Id} is already registered");
                }

                return await _deviceRepository.CreateAsync(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device with ESP32 ID: {Esp32Id}", device.Esp32Id);
                throw;
            }
        }

        public async Task<Device?> GetDeviceByEsp32IdAsync(string esp32Id)
        {
            return await _deviceRepository.GetByEsp32IdAsync(esp32Id);
        }

        public async Task<List<Device>> GetUserDevicesAsync(string userId)
        {
            return await _deviceRepository.GetByUserIdAsync(userId);
        }

        public async Task<Device?> GetDeviceByIdAsync(string id)
        {
            return await _deviceRepository.GetByIdAsync(id);
        }

        public async Task UpdateDeviceAsync(string id, Device device)
        {
            await _deviceRepository.UpdateAsync(id, device);
        }

        public async Task<bool> ValidateDeviceAsync(string esp32Id)
        {
            var device = await _deviceRepository.GetByEsp32IdAsync(esp32Id);
            if (device == null || !device.IsActive)
            {
                return false;
            }

            await _deviceRepository.UpdateLastSeenAsync(esp32Id);
            return true;
        }
    }
} 