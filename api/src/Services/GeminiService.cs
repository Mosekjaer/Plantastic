using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using api.Models;
using api.Configuration;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly GeminiSettings _settings;
        private const string API_BASE_URL = "https://openrouter.ai/api/v1/chat/completions";

        public GeminiService(
            ILogger<GeminiService> logger,
            IOptions<GeminiSettings> settings,
            HttpClient httpClient)
        {
            _logger = logger;
            _settings = settings.Value;
            _httpClient = httpClient;
        }

        public async Task<PlantHealthAnalysis> AnalyzePlantHealthAsync(List<SensorData> sensorDataList, Device device, string language)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? _settings.ApiKey;
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://plantastic.app");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "Plantastic");

                var plantName = device.Name;
                
                // Format sensor data points with timestamps
                var sensorDataText = string.Join("\n", sensorDataList.Select(data =>
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime;
                    var sensorLines = new List<string>();
                    
                    sensorLines.Add($"Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                    
                    // Only include sensors that the user has enabled
                    if (device.IncludeLightSensor)
                        sensorLines.Add($"    - Light Level: {data.Light} lux");
                        
                    if (device.IncludeMoistureSensor)
                        sensorLines.Add($"    - Soil Moisture: {data.SoilMoisture}%");
                        
                    if (device.IncludeTemperatureSensor)
                        sensorLines.Add($"    - Temperature: {data.Temperature}°C");
                        
                    if (device.IncludeHumiditySensor)
                        sensorLines.Add($"    - Humidity: {data.Humidity}%");
                        
                    if (device.IncludeSaltSensor)
                        sensorLines.Add($"    - Salt Level: {data.Salt}");
                        
                    if (device.IncludeBatterySensor)
                        sensorLines.Add($"    - Battery Level: {data.Battery}%");
                        
                    return string.Join("\n", sensorLines);
                }));

                var request = new
                {
                    model = "google/gemini-2.0-flash-exp:free",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $@"You are a friendly and knowledgeable plant care expert. Analyze the sensor data history for this {plantName} and provide a detailed, caring assessment in {language} language.

                            Plant: {plantName}
                            Time Period: {sensorDataList.Count} measurements over {(DateTimeOffset.FromUnixTimeSeconds(sensorDataList.Last().Timestamp) - DateTimeOffset.FromUnixTimeSeconds(sensorDataList.First().Timestamp)).TotalHours:F1} hours

                            Sensor Data History:
                            {sensorDataText}

                            Consider the specific needs of {plantName} and provide a thorough analysis that would be helpful and reassuring to a plant owner.
                            Analyze trends over time and identify any concerning patterns.
                            If there are issues, explain them clearly but gently, and provide specific, actionable recommendations.
                            
                            For the health status, use friendly phrases like:
                            - 'Happy and thriving!'
                            - 'Doing well, but could use some minor adjustments'
                            - 'Needs a little extra care and attention'
                            - 'Could use some help to get back to optimal health'
                            
                            For issues and recommendations, be specific and encouraging, explaining why each adjustment will help.
                            
                            Please provide the response in {language} language."
                        }
                    },
                    response_format = new
                    {
                        type = "json_schema",
                        json_schema = new
                        {
                            name = "plant_health_analysis",
                            strict = true,
                            schema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    needs_attention = new { type = "boolean" },
                                    health_status = new { type = "string" },
                                    issues = new
                                    {
                                        type = "array",
                                        items = new { type = "string" }
                                    },
                                    recommendations = new
                                    {
                                        type = "array",
                                        items = new { type = "string" }
                                    }
                                },
                                required = new[] { "needs_attention", "health_status", "issues", "recommendations" },
                                additionalProperties = false
                            }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(API_BASE_URL, request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenRouter API error response: {ErrorContent}", errorContent);
                }
                
                response.EnsureSuccessStatusCode();

                var openRouterResponse = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
                var jsonResponse = openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    throw new Exception("Failed to get valid response from OpenRouter");
                }

                return JsonSerializer.Deserialize<PlantHealthAnalysis>(jsonResponse)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing plant health with OpenRouter for plant: {PlantName}", device.Name);
                throw;
            }
        }
    }

    public class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class PlantHealthAnalysis
    {
        [JsonPropertyName("needs_attention")]
        public bool NeedsAttention { get; set; }

        [JsonPropertyName("health_status")]
        public string HealthStatus { get; set; } = string.Empty;

        [JsonPropertyName("issues")]
        public List<string> Issues { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
    }
} 