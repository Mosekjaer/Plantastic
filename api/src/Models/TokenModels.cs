using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }

    public class RefreshToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 