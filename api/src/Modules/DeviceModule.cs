using Carter;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Modules
{
    public class DeviceModule : CarterModule
    {
        public DeviceModule() : base("/devices")
        {
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/claim", async (ClaimDeviceRequest request, IDeviceService deviceService, ILogger<DeviceModule> logger, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }

                    var device = await deviceService.GetDeviceByEsp32IdAsync(request.Esp32Id);
                    if (device == null)
                    {
                        return Results.NotFound(new { message = "Device not found" });
                    }

                    if (!string.IsNullOrEmpty(device.UserId))
                    {
                        return Results.Conflict(new { message = "Device is already claimed" });
                    }

                    device.UserId = userId;

                    await deviceService.UpdateDeviceAsync(device.Id!, device);
                    return Results.Ok(new { message = "Device claimed successfully" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error claiming device");
                    return Results.Problem("An error occurred while claiming the device.");
                }
            })
            .WithName("ClaimDevice")
            .WithTags("Devices")
            .RequireAuthorization();

            app.MapGet("/", async (IDeviceService deviceService, ILogger<DeviceModule> logger, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }

                    var devices = await deviceService.GetDevicesByUserIdAsync(userId);
                    return Results.Ok(devices);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving user devices");
                    return Results.Problem("An error occurred while retrieving devices.");
                }
            })
            .WithName("GetUserDevices")
            .WithTags("Devices")
            .RequireAuthorization();
        }
    }

    public record RegisterEsp32Request(string Esp32Id, string PlantName);
    public record ClaimDeviceRequest(string Esp32Id);
} 