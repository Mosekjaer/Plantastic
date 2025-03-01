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
        public string Esp32Id { get; set; }

        [BsonElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [BsonElement("user_id")]
        public string UserId { get; set; }

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("last_seen")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastSeen { get; set; }

        [BsonElement("is_active")]
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;
    }
} 