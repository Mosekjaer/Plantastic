using api.Configuration;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace api.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly IMongoCollection<Device> _deviceCollection;

        public DeviceRepository(IOptions<MongoDBSensorSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _deviceCollection = mongoDatabase.GetCollection<Device>("Devices");
        }

        public async Task<Device> CreateAsync(Device device)
        {
            await _deviceCollection.InsertOneAsync(device);
            return device;
        }

        public async Task<Device?> GetByIdAsync(string id)
        {
            return await _deviceCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Device?> GetByEsp32IdAsync(string esp32Id)
        {
            return await _deviceCollection.Find(x => x.Esp32Id == esp32Id).FirstOrDefaultAsync();
        }

        public async Task<List<Device>> GetByUserIdAsync(string userId)
        {
            return await _deviceCollection.Find(x => x.UserId == userId).ToListAsync();
        }

        public async Task<List<Device>> GetAllAsync()
        {
            return await _deviceCollection.Find(FilterDefinition<Device>.Empty).ToListAsync();
        }

        public async Task UpdateAsync(string id, Device device)
        {
            await _deviceCollection.ReplaceOneAsync(x => x.Id == id, device);
        }

        public async Task UpdateLastSeenAsync(string esp32Id)
        {
            var update = Builders<Device>.Update.Set(x => x.LastSeen, DateTime.UtcNow);
            await _deviceCollection.UpdateOneAsync(x => x.Esp32Id == esp32Id, update);
        }
    }
} 