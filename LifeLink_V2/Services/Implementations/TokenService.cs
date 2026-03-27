using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LifeLink_V2.Data;
using LifeLink_V2.Models;
using LifeLink_V2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LifeLink_V2.Services.Implementations
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, AppDbContext context, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public string GenerateJwtToken(User user, DateTime? expiry = null)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured")));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim("IsActive", user.IsActive.ToString())
            };

            // Add patient or provider specific claims
            if (user.Patient != null)
            {
                claims.Add(new Claim("PatientId", user.Patient.PatientId.ToString()));
            }

            if (user.Provider != null)
            {
                claims.Add(new Claim("ProviderId", user.Provider.ProviderId.ToString()));
                claims.Add(new Claim("ProviderName", user.Provider.ProviderName));
                claims.Add(new Claim("ProviderTypeId", user.Provider.ProviderTypeId.ToString()));
            }

            // Add city information if available
            if (user.CityId.HasValue)
            {
                claims.Add(new Claim("CityId", user.CityId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiry ?? DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["ExpireDays"] ?? "7")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User?> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSettings = _configuration.GetSection("Jwt");
                var key = Encoding.UTF8.GetBytes(
                    jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Validate token
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Extract user ID from claims
                var userIdClaim = principal.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid UserId claim in token");
                    return null;
                }

                // Get user with related data
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Patient)
                    .Include(u => u.Provider)
                    .Include(u => u.City)
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogWarning("User not found or inactive for UserId: {UserId}", userId);
                    return null;
                }

                return user;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("JWT token expired");
                return null;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Invalid JWT token signature");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token");
                return null;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(
                jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = false, // We're accepting expired tokens here
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        // Implement the single-argument overload to delegate to the main method
        public string GenerateJwtToken(User user)
        {
            return GenerateJwtToken(user, null);
        }
    }
}