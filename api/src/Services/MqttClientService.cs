using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using api.Configuration;

namespace api.Services
{
    public class MqttClientService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly ISensorDataService _sensorDataService;
        private readonly ILogger<MqttClientService> _logger;
        private readonly MqttSettings _mqttSettings;

        public MqttClientService(
            ISensorDataService sensorDataService,
            ILogger<MqttClientService> logger,
            IOptions<MqttSettings> mqttSettings)
        {
            _sensorDataService = sensorDataService;
            _logger = logger;
            _mqttSettings = mqttSettings.Value;
            _mqttClient = new MqttFactory().CreateMqttClient();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .WithCredentials(_mqttSettings.Username, _mqttSettings.Password)
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true
                })
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            try 
            {
                await _mqttClient.ConnectAsync(options, cancellationToken);

                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic("sensor/+/status") 
                    .Build();

                await _mqttClient.SubscribeAsync(topicFilter, cancellationToken);
                
                _logger.LogInformation("MQTT client connected and subscribed to topics");
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
                var esp32Id = e.ApplicationMessage.Topic.Split('/')[1];
                
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var sensorData = JsonSerializer.Deserialize<SensorData>(payload);

                if (sensorData != null)
                {
                    await _sensorDataService.ProcessSensorDataAsync(esp32Id, sensorData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
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
} 