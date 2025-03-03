using api.Models;

namespace api.Interfaces
{
    public interface IUserService
    {
        Task<string?> GetUserEmailById(string userId);
        Task<ApplicationUser?> GetUserById(string userId);
        Task<List<ApplicationUser>> GetAllUsersAsync();
    }
} 