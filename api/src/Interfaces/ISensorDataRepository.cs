using api.Models;

namespace api.Interfaces
{
    public interface ISensorDataRepository
    {
        Task CreateAsync(SensorData sensorData);
        Task<SensorData?> GetByIdAsync(string id);
        Task<List<SensorData>> GetAllAsync();
    }
}
