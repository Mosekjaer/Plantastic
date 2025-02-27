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
        public int Light { get; set; }

        [BsonElement("soil_moisture")]
        [JsonPropertyName("soil_moisture")]
        public int SoilMoisture { get; set; }

        [BsonElement("salt")]
        public int Salt { get; set; }

        [BsonElement("temperature")]
        public int Temperature { get; set; }

        [BsonElement("humidity")]
        public int Humidity { get; set; }

        [BsonElement("battery")]
        public int Battery { get; set; }

        [BsonElement("plant_name")]
        [JsonPropertyName("plant_name")]
        public string? PlantName { get; set; }

        [BsonElement("timestamp")]
        public long Timestamp { get; set; } 

        [BsonElement("esp32_id")]
        public string? Esp32Id { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
