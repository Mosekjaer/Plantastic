using api.Configuration;
using api.Interfaces;
using api.Repositories;
using api.Services;

namespace api.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.Configure<MongoDBSettings>(options =>
            {
                var host = Environment.GetEnvironmentVariable("MONGODB_HOST") ?? "localhost";
                var port = Environment.GetEnvironmentVariable("MONGODB_PORT") ?? "27017";
                var username = Environment.GetEnvironmentVariable("MONGODB_USERNAME");
                var password = Environment.GetEnvironmentVariable("MONGODB_PASSWORD");
                
                options.ConnectionString = string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)
                    ? $"mongodb://{host}:{port}"
                    : $"mongodb://{username}:{password}@{host}:{port}";
                
                options.DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? "sensor_data_db";
                options.CollectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION") ?? "sensor_readings";
            });

            services.Configure<MqttSettings>(options =>
            {
                options.Host = Environment.GetEnvironmentVariable("MQTT_HOST") ?? string.Empty;
                options.Port = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "1883");
                options.Username = Environment.GetEnvironmentVariable("MQTT_USERNAME") ?? string.Empty;
                options.Password = Environment.GetEnvironmentVariable("MQTT_PASSWORD") ?? string.Empty;
            });

            services.AddSingleton<ISensorDataRepository, SensorDataRepository>();
            services.AddSingleton<ISensorDataService, SensorDataService>();
            services.AddHostedService<MqttClientService>();
        }

        public static void AddOpenApi(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
        }

        public static void MapOpenApi(this IApplicationBuilder app)
        {

        }
    }
}
