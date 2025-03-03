using api.Models;

namespace api.Interfaces
{
    public interface IDeviceService
    {
        Task<Device?> GetDeviceByIdAsync(string id);
        Task<Device?> GetDeviceByEsp32IdAsync(string esp32Id);
        Task<List<Device>> GetDevicesByUserIdAsync(string userId);
        Task<bool> ValidateDeviceAsync(string esp32Id);
        Task<Device> RegisterDeviceAsync(Device device);
        Task UpdateDeviceAsync(string id, Device device);
    }
} 