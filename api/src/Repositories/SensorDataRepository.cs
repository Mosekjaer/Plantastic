using api.Configuration;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace api.Repositories
{
    public class SensorDataRepository : ISensorDataRepository
    {
        private readonly IMongoCollection<SensorData> _sensorDataCollection;

        public SensorDataRepository(IOptions<MongoDBSensorSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _sensorDataCollection = mongoDatabase.GetCollection<SensorData>(mongoDBSettings.Value.CollectionName);
        }

        public async Task CreateAsync(SensorData sensorData)
        {
            await _sensorDataCollection.InsertOneAsync(sensorData);
        }

        public async Task<List<SensorData>> GetAllAsync()
        {
            return await _sensorDataCollection.Find(FilterDefinition<SensorData>.Empty).ToListAsync();
        }

        public async Task<SensorData?> GetByIdAsync(string id)
        {
            return await _sensorDataCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }
    }
}
