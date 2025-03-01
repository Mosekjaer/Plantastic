using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using api.Configuration;
using System.Text.Json.Serialization;

namespace api.Services
{
    public class MqttClientService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly ISensorDataService _sensorDataService;
        private readonly IDeviceService _deviceService;
        private readonly ILogger<MqttClientService> _logger;
        private readonly MqttSettings _mqttSettings;

        public MqttClientService(
            ISensorDataService sensorDataService,
            IDeviceService deviceService,
            ILogger<MqttClientService> logger,
            IOptions<MqttSettings> mqttSettings)
        {
            _sensorDataService = sensorDataService;
            _deviceService = deviceService;
            _logger = logger;
            _mqttSettings = mqttSettings.Value;
            _mqttClient = new MqttFactory().CreateMqttClient();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .WithCredentials(_mqttSettings.Username, _mqttSettings.Password)
                .WithClientId($"plantastic-server-{Guid.NewGuid()}")
                .WithCleanSession(false) 
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true
                })
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;
            _mqttClient.DisconnectedAsync += HandleDisconnectedAsync;

            try 
            {
                await _mqttClient.ConnectAsync(options, cancellationToken);

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => 
                    {
                        f.WithTopic("sensor/+/status")
                         .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                    })
                    .WithTopicFilter(f => 
                    {
                        f.WithTopic("sensor/+/register")
                         .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                    })
                    .Build();

                await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
                
                _logger.LogInformation("MQTT client connected and subscribed to topics with QoS 1");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MQTT broker");
                throw;
            }
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topicParts = e.ApplicationMessage.Topic.Split('/');
                var esp32Id = topicParts[1];
                var messageType = topicParts[2];
                
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                if (messageType == "register")
                {
                    await HandleRegistrationMessage(esp32Id, payload);
                }
                else if (messageType == "status")
                {
                    await HandleSensorDataMessage(esp32Id, payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
            }
        }

        private Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("Disconnected from MQTT broker: {Reason}", args.Reason);
            
            Task.Run(async () =>
            {
                var retryCount = 0;
                var maxRetries = 10;
                var delay = TimeSpan.FromSeconds(1);

                while (retryCount < maxRetries && !_mqttClient.IsConnected)
                {
                    try
                    {
                        retryCount++;
                        _logger.LogInformation("Attempting to reconnect... Attempt {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                        await _mqttClient.ReconnectAsync();
                        _logger.LogInformation("Successfully reconnected to MQTT broker");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to reconnect");
                        await Task.Delay(delay);
                        delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60)); 
                    }
                }
            });

            return Task.CompletedTask;
        }

        private async Task HandleRegistrationMessage(string esp32Id, string payload)
        {
            try
            {
                _logger.LogInformation("Received registration payload: {Payload}", payload);
                var registrationData = JsonSerializer.Deserialize<RegisterEsp32Request>(payload);
                if (registrationData == null)
                {
                    _logger.LogWarning("Invalid registration data received for ESP32 ID: {Esp32Id}", esp32Id);
                    return;
                }

                var device = new Device
                {
                    Esp32Id = esp32Id,
                    Name = registrationData.PlantName,
                    IsActive = true
                };

                await _deviceService.RegisterDeviceAsync(device);
                _logger.LogInformation("Device registered successfully: {Esp32Id} with name: {PlantName}", esp32Id, device.Name);

                var responseTopic = $"sensor/{esp32Id}/register/response";
                var responseMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(responseTopic)
                    .WithPayload(JsonSerializer.Serialize(new { success = true, message = "Device registered successfully" }))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(true)
                    .Build();

                await _mqttClient.PublishAsync(responseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing registration for ESP32 ID: {Esp32Id}", esp32Id);
                
                // Send error response with QoS 1
                var responseTopic = $"sensor/{esp32Id}/register/response";
                var responseMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(responseTopic)
                    .WithPayload(JsonSerializer.Serialize(new { success = false, message = ex.Message }))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(true) 
                    .Build();

                await _mqttClient.PublishAsync(responseMessage);
            }
        }

        private async Task HandleSensorDataMessage(string esp32Id, string payload)
        {
            var sensorData = JsonSerializer.Deserialize<SensorData>(payload);
            if (sensorData != null)
            {
                await _sensorDataService.ProcessSensorDataAsync(esp32Id, sensorData);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient.IsConnected)
            {
                var disconnectOptions = new MqttClientDisconnectOptionsBuilder().Build();
                await _mqttClient.DisconnectAsync(disconnectOptions, cancellationToken);
            }
        }
    }

    public class RegisterEsp32Request
    {
        [JsonPropertyName("plantName")]
        public string PlantName { get; set; } = string.Empty;

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;
    }
} 