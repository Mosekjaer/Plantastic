using api.Configuration;
using api.Interfaces;
using api.Repositories;
using api.Services;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;

namespace api.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            var mongoHost = Environment.GetEnvironmentVariable("MONGODB_HOST") ?? "localhost";
            var mongoPort = Environment.GetEnvironmentVariable("MONGODB_PORT") ?? "27017";
            var mongoUsername = Environment.GetEnvironmentVariable("MONGODB_USERNAME");
            var mongoPassword = Environment.GetEnvironmentVariable("MONGODB_PASSWORD");
            var mongoConnectionString = string.IsNullOrEmpty(mongoUsername) || string.IsNullOrEmpty(mongoPassword)
                    ? $"mongodb://{mongoHost}:{mongoPort}"
                    : $"mongodb://{mongoUsername}:{mongoPassword}@{mongoHost}:{mongoPort}";

            services.Configure<MongoDBSensorSettings>(options =>
            {
                options.ConnectionString = mongoConnectionString;
                options.DatabaseName = Environment.GetEnvironmentVariable("MONGODB_SENSOR_DATABASE") ?? "sensor_data_db";
                options.CollectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION") ?? "sensor_readings";
            });

            services.Configure<MongoDBUserSettings>(options =>
            {
                options.ConnectionString = mongoConnectionString;
                options.DatabaseName = Environment.GetEnvironmentVariable("MONGODB_USER_DATABASE") ?? "sensor_data_db";
            });

            services.Configure<GeminiSettings>(options =>
            {
                options.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
                options.Model = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.0-flash-exp";
            });

            services.Configure<EmailSettings>(options =>
            {
                options.Username = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? string.Empty;
                options.AppPassword = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD") ?? string.Empty;
                options.SmtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST") ?? "smtp.gmail.com";
                options.SmtpPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? "587");
            });

            BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddMongoDbStores<ApplicationUser, ApplicationRole, string>(
                    mongoConnectionString,
                    Environment.GetEnvironmentVariable("MONGODB_USER_DATABASE"))
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.Configure<MqttSettings>(options =>
            {
                options.Host = Environment.GetEnvironmentVariable("MQTT_HOST") ?? string.Empty;
                options.Port = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "1883");
                options.Username = Environment.GetEnvironmentVariable("MQTT_USERNAME") ?? string.Empty;
                options.Password = Environment.GetEnvironmentVariable("MQTT_PASSWORD") ?? string.Empty;
            });

            services.Configure<JwtSettings>(options =>
            {
                options.SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-super-secret-key-with-at-least-32-characters";
                options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "plantastic";
                options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "plantastic-api";
                options.AccessTokenExpirationMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES") ?? "15");
                options.RefreshTokenExpirationDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_DAYS") ?? "7");
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = services.BuildServiceProvider().GetRequiredService<IOptions<JwtSettings>>().Value;
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddScoped<ITokenService, TokenService>();

            services.AddSingleton<IMongoClient>(sp =>
            {
                return new MongoClient(mongoConnectionString);
            });

            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var dbName = Environment.GetEnvironmentVariable("MONGODB_SENSOR_DATABASE") ?? "sensor_data_db";
                return client.GetDatabase(dbName);
            });

            services.AddSingleton<ISensorDataRepository, SensorDataRepository>();
            services.AddSingleton<ISensorDataService, SensorDataService>();
            services.AddSingleton<IDeviceRepository, DeviceRepository>();
            services.AddSingleton<IDeviceService, DeviceService>();
            services.AddSingleton<IDeviceNotificationRepository, DeviceNotificationRepository>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<GeminiService>();
            services.AddSingleton<EmailService>();
            services.AddHostedService<MqttClientService>();
            services.AddHttpClient();
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
