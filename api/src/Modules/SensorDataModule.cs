using System.Text.Json;
using api.Interfaces;
using api.Models;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace api.Modules
{
    public class SensorDataModule : CarterModule
    {
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/sensor-data/{esp32Id}", async (string esp32Id, [FromBody] SensorData sensorData, ISensorDataService sensorDataService, ILogger<SensorDataModule> logger) =>
            {
                try
                {
                    if (sensorData == null)
                    {
                        logger.LogError("Received null sensor data for ESP32 ID: {Esp32Id}", esp32Id);
                        return Results.BadRequest("Sensor data cannot be null.");
                    }

                    await sensorDataService.ProcessSensorDataAsync(esp32Id, sensorData);
                    return Results.Accepted(); 
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing sensor data for ESP32 ID: {Esp32Id}", esp32Id);
                    return Results.Problem(detail: "An error occurred while processing the sensor data.", statusCode: 500);
                }
            })
            .WithName("PostSensorData")
            .WithTags("SensorData")
            .RequireAuthorization();

            app.MapGet("/sensor-data/", async (ISensorDataService sensorDataService, ILogger<SensorDataModule> logger) =>
            {
                try
                {
                    var result = await sensorDataService.GetAllSensorDataAsync();
                    if (!result.Any())
                    {
                        return Results.NotFound();
                    }
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting sensor data");
                    return Results.Problem(detail: "An error occurred while processing the sensor data.", statusCode: 500);
                }
            })
            .WithName("ReceiveSensorData")
            .WithTags("SensorData")
            .RequireAuthorization();
        }
    }
}
