using api.Models;

namespace api.Interfaces
{
    public interface IDeviceRepository
    {
        Task<Device> CreateAsync(Device device);
        Task<Device?> GetByIdAsync(string id);
        Task<Device?> GetByEsp32IdAsync(string esp32Id);
        Task<List<Device>> GetByUserIdAsync(string userId);
        Task<List<Device>> GetAllAsync();
        Task UpdateAsync(string id, Device device);
        Task UpdateLastSeenAsync(string esp32Id);
    }
} 