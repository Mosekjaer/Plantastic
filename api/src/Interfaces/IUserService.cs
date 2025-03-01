using api.Models;

namespace api.Interfaces
{
    public interface IUserService
    {
        Task<string?> GetUserEmailById(string userId);
    }
} 