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
        private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";

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
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _settings.ApiKey;
                var url = $"{API_BASE_URL}{_settings.Model}:generateContent?key={apiKey}";

                var plantName = device.Name;
                
                // Format sensor data points with timestamps
                var sensorDataText = string.Join("\n", sensorDataList.Select(data =>
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime;
                    return $@"Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
    - Light Level: {data.Light} lux
    - Soil Moisture: {data.SoilMoisture}%
    - Temperature: {data.Temperature}°C
    - Humidity: {data.Humidity}%";
    //TODO: Support this later on.
    //- Salt Level: {data.Salt}
    //- Battery Level: {data.Battery}%";
                }));

                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $@"You are a friendly and knowledgeable plant care expert. Analyze the sensor data history for this {plantName} and provide a detailed, caring assessment in {language} language.

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
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 32,
                        topP = 1,
                        maxOutputTokens = 2048,
                        candidateCount = 1,
                        responseMimeType = "application/json",
                        responseSchema = new
                        {
                            type = "OBJECT",
                            properties = new
                            {
                                needs_attention = new { type = "BOOLEAN" },
                                health_status = new { type = "STRING" },
                                issues = new
                                {
                                    type = "ARRAY",
                                    items = new { type = "STRING" }
                                },
                                recommendations = new
                                {
                                    type = "ARRAY",
                                    items = new { type = "STRING" }
                                }
                            },
                            required = new[] { "needs_attention", "health_status", "issues", "recommendations" }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error response: {ErrorContent}", errorContent);
                }
                
                response.EnsureSuccessStatusCode();

                var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                var jsonResponse = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    throw new Exception("Failed to get valid response from Gemini");
                }

                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
                
                return JsonSerializer.Deserialize<PlantHealthAnalysis>(jsonResponse)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing plant health with Gemini for plant: {PlantName}", device.Name);
                throw;
            }
        }
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
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