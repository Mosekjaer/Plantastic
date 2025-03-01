using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using api.Configuration;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace api.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IMongoCollection<RefreshToken> _refreshTokens;
        private readonly IMongoCollection<ApplicationUser> _users;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IOptions<MongoDBSettings> mongoSettings)
        {
            _jwtSettings = jwtSettings.Value;
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _refreshTokens = database.GetCollection<RefreshToken>("refresh_tokens");
            _users = database.GetCollection<ApplicationUser>("users");
        }

        public async Task<TokenResponse> GenerateTokensAsync(ApplicationUser user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokens.InsertOneAsync(refreshTokenEntity);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                RefreshTokenExpiration = refreshTokenEntity.ExpiryDate
            };
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _refreshTokens.Find(x => x.Token == refreshToken && !x.IsRevoked).FirstOrDefaultAsync();
            
            if (storedToken == null || storedToken.ExpiryDate < DateTime.UtcNow)
                throw new SecurityTokenException("Invalid refresh token");

            var user = await _users.Find(x => x.Id == storedToken.UserId).FirstOrDefaultAsync();
            if (user == null)
                throw new SecurityTokenException("User not found");

            // Revoke the old refresh token
            await RevokeRefreshTokenAsync(refreshToken);

            // Generate new tokens
            return await GenerateTokensAsync(user);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var update = Builders<RefreshToken>.Update.Set(x => x.IsRevoked, true);
            await _refreshTokens.UpdateOneAsync(x => x.Token == refreshToken, update);
        }

        private string GenerateAccessToken(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
} 