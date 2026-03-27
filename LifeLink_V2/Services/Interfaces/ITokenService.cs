using LifeLink_V2.Models;
using System.Security.Claims;

namespace LifeLink_V2.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user, DateTime? expiry = null);
        Task<User?> ValidateTokenAsync(string token);

        // ADD THESE OPTIONAL METHODS:
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}