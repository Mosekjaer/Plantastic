using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
namespace api.Models
{
    [CollectionName("Users")]
    public class ApplicationUser : MongoIdentityUser<string>
    {
        public string FullName { get; set; }
        
        // "en", "es", "fr" etc...
        public string PreferredLanguage { get; set; } = "en";
        
        // in user's local timezone
        public TimeSpan NotificationTime { get; set; } = new TimeSpan(9, 0, 0); // Default to 9 AM
        
        // 24 for daily, 12 for twice a day
        public int NotificationIntervalHours { get; set; } = 24;
        
        // timezone offset from UTC in minutes
        public int TimezoneOffsetMinutes { get; set; }

        public ApplicationUser() : base()
        {
        }
    }
}