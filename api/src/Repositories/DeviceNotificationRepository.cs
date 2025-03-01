using api.Interfaces;
using api.Models;
using MongoDB.Driver;

namespace api.Repositories
{
    public class DeviceNotificationRepository : IDeviceNotificationRepository
    {
        private readonly IMongoCollection<DeviceNotification> _notifications;

        public DeviceNotificationRepository(IMongoDatabase database)
        {
            _notifications = database.GetCollection<DeviceNotification>("device_notifications");
        }

        public async Task<DeviceNotification?> GetByDeviceIdAsync(string deviceId)
        {
            return await _notifications.Find(n => n.DeviceId == deviceId).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(DeviceNotification notification)
        {
            await _notifications.InsertOneAsync(notification);
        }

        public async Task UpdateAsync(string id, DeviceNotification notification)
        {
            await _notifications.ReplaceOneAsync(n => n.Id == id, notification);
        }
    }
} 