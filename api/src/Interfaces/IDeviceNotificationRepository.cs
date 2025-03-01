using api.Models;

namespace api.Interfaces
{
    public interface IDeviceNotificationRepository
    {
        Task<DeviceNotification?> GetByDeviceIdAsync(string deviceId);
        Task CreateAsync(DeviceNotification notification);
        Task UpdateAsync(string id, DeviceNotification notification);
    }
} 