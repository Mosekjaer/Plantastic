using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models
{
    public class DeviceNotification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("device_id")]
        public string DeviceId { get; set; }

        [BsonElement("last_notification_sent")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastNotificationSent { get; set; }

        [BsonElement("notification_type")]
        public string NotificationType { get; set; } = "plant_health";
    }
} 