using api.Interfaces;
using api.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using api.Configuration;

namespace api.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<ApplicationUser> _users;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IMongoClient mongoClient,
            IOptions<MongoDBUserSettings> settings,
            ILogger<UserService> logger)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _users = database.GetCollection<ApplicationUser>("Users");
            _logger = logger;
        }

        public async Task<string?> GetUserEmailById(string userId)
        {
            try
            {
                var user = await _users.Find(x => x.Id == userId).FirstOrDefaultAsync();
                return user?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user email for ID: {UserId}", userId);
                return null;
            }
        }
    }
} 