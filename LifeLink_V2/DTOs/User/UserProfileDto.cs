using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.User
{
    public class UserProfileDto
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم الكامل لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        public int? CityId { get; set; }

        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string? Address { get; set; }

        public string? Role { get; set; }
        public string? CityName { get; set; }
        public string? GovernorateName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }

        // Patient specific
        public int? PatientId { get; set; }
        public string? NationalId { get; set; }
        public string? InsuranceNumber { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }

        // Provider specific
        public int? ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderType { get; set; }
        public string? MedicalLicenseNumber { get; set; }
    }

    public class UpdateProfileDto
    {
        [MaxLength(150, ErrorMessage = "الاسم الكامل لا يمكن أن يتجاوز 150 حرفًا")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        public int? CityId { get; set; }

        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string? Address { get; set; }
    }
}