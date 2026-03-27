namespace LifeLink_V2.DTOs.Auth
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public UserInfoDto? User { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public string? GovernorateName { get; set; }
        public int? PatientId { get; set; }
        public int? ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}