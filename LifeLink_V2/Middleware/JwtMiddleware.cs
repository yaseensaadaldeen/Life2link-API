
using LifeLink_V2.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LifeLink_V2.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context, ITokenService tokenService)
        {
            // Read raw Authorization header (if any)
            var rawAuth = context.Request.Headers["Authorization"].FirstOrDefault();

            string? token = null;

            if (!string.IsNullOrWhiteSpace(rawAuth))
            {
                rawAuth = rawAuth.Trim();

                // Common case: "Bearer <token>"
                if (rawAuth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = rawAuth.Substring("Bearer ".Length).Trim().Trim('"', '\'');
                }
                else
                {
                    // Defensive: try to extract a JWT-like substring (three base64url parts separated by dots)
                    var jwtMatch = Regex.Match(rawAuth, @"[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+");
                    if (jwtMatch.Success)
                    {
                        token = jwtMatch.Value.Trim().Trim('"', '\'');
                    }
                    else
                    {
                        var parts = rawAuth.Split(new[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in parts)
                        {
                            var m = Regex.Match(p, @"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$");
                            if (m.Success)
                            {
                                token = m.Value;
                                break;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                var user = await tokenService.ValidateTokenAsync(token);
                if (user != null)
                {
                    // Attach user object for app code
                    context.Items["User"] = user;

                    // Build ClaimsPrincipal so ASP.NET Core [Authorize] recognizes the authenticated user
                    var claims = new List<Claim>
                    {
                        // standard claims
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),

                        // explicit literal claims your controllers expect
                        new Claim("UserId", user.UserId.ToString()),
                        new Claim("Role", user.Role?.RoleName ?? string.Empty)
                    };

                    // keep Role also as ClaimTypes.Role for framework compatibility
                    if (user.Role != null && !string.IsNullOrWhiteSpace(user.Role.RoleName))
                        claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName));

                    if (user.Patient != null)
                        claims.Add(new Claim("PatientId", user.Patient.PatientId.ToString()));

                    if (user.Provider != null)
                    {
                        claims.Add(new Claim("ProviderId", user.Provider.ProviderId.ToString()));
                        if (!string.IsNullOrWhiteSpace(user.Provider.ProviderName))
                            claims.Add(new Claim("ProviderName", user.Provider.ProviderName));
                    }

                    var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                    context.User = new ClaimsPrincipal(identity);
                }
            }

            await _next(context);
        }
    }
}