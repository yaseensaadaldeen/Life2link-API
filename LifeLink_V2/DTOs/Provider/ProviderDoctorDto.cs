using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Provider
{
    public class ProviderDoctorDto
    {
        public int DoctorId { get; set; }
        public int ProviderId { get; set; }

        [Required(ErrorMessage = "اسم الدكتور مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        public int? SpecialtyId { get; set; }
        public string? SpecialtyName { get; set; }

        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string? Email { get; set; }

        [MaxLength(200, ErrorMessage = "ساعات العمل لا يمكن أن تتجاوز 200 حرف")]
        public string? WorkingHours { get; set; }

        [MaxLength(150, ErrorMessage = "رقم الرخصة الطبية لا يمكن أن يتجاوز 150 حرف")]
        public string? MedicalLicenseNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiry { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
    }

    public class CreateDoctorDto
    {
        [Required(ErrorMessage = "اسم الدكتور مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        public int? SpecialtyId { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string? Email { get; set; }

        [MaxLength(200, ErrorMessage = "ساعات العمل لا يمكن أن تتجاوز 200 حرف")]
        public string? WorkingHours { get; set; }

        [MaxLength(150, ErrorMessage = "رقم الرخصة الطبية لا يمكن أن يتجاوز 150 حرف")]
        public string? MedicalLicenseNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiry { get; set; }
    }

    public class UpdateDoctorDto
    {
        [MaxLength(150, ErrorMessage = "الاسم لا يمكن أن يتجاوز 150 حرفًا")]
        public string? FullName { get; set; }

        public int? SpecialtyId { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string? Email { get; set; }

        [MaxLength(200, ErrorMessage = "ساعات العمل لا يمكن أن تتجاوز 200 حرف")]
        public string? WorkingHours { get; set; }

        [MaxLength(150, ErrorMessage = "رقم الرخصة الطبية لا يمكن أن يتجاوز 150 حرف")]
        public string? MedicalLicenseNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiry { get; set; }

        public bool? IsActive { get; set; }
    }
}