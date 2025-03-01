using api.Models;

namespace api.Interfaces
{
    public interface ISensorDataService
    {
        Task<bool> ProcessSensorDataAsync(string esp32Id, SensorData sensorData);
        Task<List<SensorData>> GetAllSensorDataAsync();
    }
}
