namespace LifeLink_V2.DTOs.Auth
{
    public class RefreshTokenResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiry { get; set; }
    }
}