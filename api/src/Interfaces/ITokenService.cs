using api.Models;

namespace api.Interfaces
{
    public interface ITokenService
    {
        Task<TokenResponse> GenerateTokensAsync(ApplicationUser user);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
} 