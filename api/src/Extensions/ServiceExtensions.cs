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
                options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? string.Empty;
                options.DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME") ?? string.Empty;
                options.CollectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION_NAME") ?? string.Empty;
            });

            services.AddSingleton<ISensorDataRepository, SensorDataRepository>();
            services.AddScoped<ISensorDataService, SensorDataService>();
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
