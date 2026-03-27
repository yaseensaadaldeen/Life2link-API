using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Provider
{
    public class ProviderDto
    {
        public int ProviderId { get; set; }
        public int UserId { get; set; }

        public string ProviderName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string? OwnerPhone { get; set; }

        public int ProviderTypeId { get; set; }
        public string ProviderType { get; set; } = string.Empty;

        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public string? GovernorateName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public decimal? Rating { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalDoctors { get; set; }

        public string? MedicalLicenseNumber { get; set; }
        public string? LicenseIssuedBy { get; set; }
        public DateTime? LicenseExpiry { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }

        // Statistics
        public int TodayAppointments { get; set; }
        public int ThisMonthAppointments { get; set; }
        public int PendingAppointments { get; set; }
    }

    public class CreateProviderDto
    {
        // Owner/User Info
        [Required(ErrorMessage = "اسم المالك/المسؤول مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 8 أحرف")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        public int GovernorateId { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string Address { get; set; } = string.Empty;

        // Provider Info
        [Required(ErrorMessage = "اسم المؤسسة مطلوب")]
        [MaxLength(250, ErrorMessage = "اسم المؤسسة لا يمكن أن يتجاوز 250 حرف")]
        public string ProviderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع المؤسسة مطلوب")]
        public int ProviderTypeId { get; set; }

        [Required(ErrorMessage = "رقم الرخصة الطبية مطلوب")]
        [MaxLength(150, ErrorMessage = "رقم الرخصة الطبية لا يمكن أن يتجاوز 150 حرف")]
        public string MedicalLicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "جهة إصدار الرخصة مطلوبة")]
        [MaxLength(250, ErrorMessage = "جهة إصدار الرخصة لا يمكن أن تتجاوز 250 حرف")]
        public string LicenseIssuedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ انتهاء الرخصة مطلوب")]
        [DataType(DataType.Date)]
        public DateTime LicenseExpiry { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني للمؤسسة غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني للمؤسسة لا يمكن أن يتجاوز 150 حرف")]
        public string? ProviderEmail { get; set; }

        [Phone(ErrorMessage = "صيغة رقم هاتف المؤسسة غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم هاتف المؤسسة لا يمكن أن يتجاوز 30 رقمًا")]
        public string? ProviderPhone { get; set; }

        [MaxLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف")]
        public string? Description { get; set; }
    }

    public class UpdateProviderDto
    {
        [MaxLength(250, ErrorMessage = "اسم المؤسسة لا يمكن أن يتجاوز 250 حرف")]
        public string? ProviderName { get; set; }

        [Phone(ErrorMessage = "صيغة رقم هاتف المؤسسة غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم هاتف المؤسسة لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني للمؤسسة غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني للمؤسسة لا يمكن أن يتجاوز 150 حرف")]
        public string? Email { get; set; }

        public int? CityId { get; set; }

        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string? Address { get; set; }

        [MaxLength(150, ErrorMessage = "رقم الرخصة الطبية لا يمكن أن يتجاوز 150 حرف")]
        public string? MedicalLicenseNumber { get; set; }

        [MaxLength(250, ErrorMessage = "جهة إصدار الرخصة لا يمكن أن تتجاوز 250 حرف")]
        public string? LicenseIssuedBy { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiry { get; set; }

        [MaxLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ProviderSearchDto
    {
        public string? SearchTerm { get; set; }
        public int? ProviderTypeId { get; set; }
        public int? CityId { get; set; }
        public bool? IsActive { get; set; }
        public decimal? MinRating { get; set; }
        public string? MedicalLicenseNumber { get; set; }
        public bool? HasAvailableDoctors { get; set; }
    }
}