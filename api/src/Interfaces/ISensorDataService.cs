using api.Models;

namespace api.Interfaces
{
    public interface ISensorDataService
    {
        Task<List<SensorData>> GetAllSensorDataAsync();
        Task<bool> ProcessSensorDataAsync(string esp32Id, SensorData sensorData);
        Task AnalyzeDeviceDataAsync(string deviceId, DateTime analysisStartTime);
    }
}
