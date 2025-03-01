using api.Extensions;
using api.Models;
using Carter;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 1_073_741_824,
        retainedFileCountLimit: 31,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.Seq("http://seq:5341") 
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
builder.Services.AddCarter();
builder.Services.AddApplicationServices();
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();
app.MapCarter();

app.Run();
