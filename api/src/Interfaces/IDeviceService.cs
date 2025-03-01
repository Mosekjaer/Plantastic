using api.Models;

namespace api.Interfaces
{
    public interface IDeviceService
    {
        Task<Device> RegisterDeviceAsync(Device device);
        Task<Device?> GetDeviceByEsp32IdAsync(string esp32Id);
        Task<List<Device>> GetUserDevicesAsync(string userId);
        Task<Device?> GetDeviceByIdAsync(string id);
        Task UpdateDeviceAsync(string id, Device device);
        Task<bool> ValidateDeviceAsync(string esp32Id);
    }
} 