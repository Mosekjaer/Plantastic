using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace api.Models
{
    public class Device
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("esp32_id")]
        [JsonPropertyName("esp32_id")]
        public string Esp32Id { get; set; } = string.Empty;

        [BsonElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("user_id")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("last_seen")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        [BsonElement("is_active")]
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        // Sensor inclusion preferences
        public bool IncludeLightSensor { get; set; } = true;
        public bool IncludeMoistureSensor { get; set; } = true;
        public bool IncludeTemperatureSensor { get; set; } = true;
        public bool IncludeHumiditySensor { get; set; } = true;
        public bool IncludeSaltSensor { get; set; } = true;
        public bool IncludeBatterySensor { get; set; } = true;
    }
} 