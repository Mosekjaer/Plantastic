using api.Models;

namespace api.Interfaces
{
    public interface ISensorDataService
    {
        Task ProcessSensorDataAsync(string esp32Id, SensorData sensorData);
        Task<List<SensorData>> GetAllSensorDataAsync();
    }
}
