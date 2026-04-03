using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Auth
{
    public class VerifyEmailDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}