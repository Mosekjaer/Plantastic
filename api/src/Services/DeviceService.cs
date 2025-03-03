using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IDeviceRepository deviceRepository,
            ILogger<DeviceService> logger)
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

        public async Task<Device?> GetDeviceByIdAsync(string id)
        {
            try
            {
                return await _deviceRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device by ID: {DeviceId}", id);
                return null;
            }
        }

        public async Task<Device?> GetDeviceByEsp32IdAsync(string esp32Id)
        {
            try
            {
                return await _deviceRepository.GetByEsp32IdAsync(esp32Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device by ESP32 ID: {Esp32Id}", esp32Id);
                return null;
            }
        }

        public async Task<List<Device>> GetDevicesByUserIdAsync(string userId)
        {
            try
            {
                return await _deviceRepository.GetByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices for user: {UserId}", userId);
                return new List<Device>();
            }
        }

        public async Task UpdateDeviceAsync(string id, Device device)
        {
            await _deviceRepository.UpdateAsync(id, device);
        }

        public async Task<bool> ValidateDeviceAsync(string esp32Id)
        {
            try
            {
                var device = await _deviceRepository.GetByEsp32IdAsync(esp32Id);
                if (device == null)
                {
                    _logger.LogWarning("Device not found: {Esp32Id}", esp32Id);
                    return false;
                }

                if (!device.IsActive)
                {
                    _logger.LogWarning("Device is inactive: {Esp32Id}", esp32Id);
                    return false;
                }

                await _deviceRepository.UpdateLastSeenAsync(esp32Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating device: {Esp32Id}", esp32Id);
                return false;
            }
        }
    }
} 