using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace api.Models
{
    public class SensorData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("light")]
        [JsonPropertyName("light")]
        public double Light { get; set; }

        [BsonElement("soil_moisture")]
        [JsonPropertyName("soil_moisture")]
        public int SoilMoisture { get; set; }

        [BsonElement("salt")]
        [JsonPropertyName("salt")]
        public int Salt { get; set; }

        [BsonElement("temperature")]
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [BsonElement("humidity")]
        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [BsonElement("battery")]
        [JsonPropertyName("battery")]
        public int Battery { get; set; }

        [BsonElement("plant_name")]
        [JsonPropertyName("plant_name")]
        public string? PlantName { get; set; }

        [BsonElement("timestamp")]
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; } 

        [BsonElement("esp32_id")]
        public string? Esp32Id { get; set; }

        [BsonElement("device_id")]
        public string? DeviceId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
