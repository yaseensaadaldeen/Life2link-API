using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Patient
{
    public class PatientDto
    {
        public int PatientId { get; set; }
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public string? GovernorateName { get; set; }

        public string? NationalId { get; set; }
        public int? InsuranceCompanyId { get; set; }
        public string? InsuranceCompanyName { get; set; }
        public string? InsuranceNumber { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalOrders { get; set; }
    }

    public class CreatePatientDto
    {
        // User Info
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم الكامل لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        public int GovernorateId { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int CityId { get; set; }

        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string? Address { get; set; }

        // Patient Info
        [Required(ErrorMessage = "رقم الهوية الوطنية مطلوب")]
        [MaxLength(100, ErrorMessage = "رقم الهوية الوطنية لا يمكن أن يتجاوز 100 حرف")]
        public string NationalId { get; set; } = string.Empty;

        public int? InsuranceCompanyId { get; set; }

        [MaxLength(100, ErrorMessage = "رقم التأمين لا يمكن أن يتجاوز 100 حرف")]
        public string? InsuranceNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [MaxLength(10, ErrorMessage = "الجنس لا يمكن أن يتجاوز 10 أحرف")]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(5, ErrorMessage = "فصيلة الدم لا يمكن أن تتجاوز 5 أحرف")]
        public string? BloodType { get; set; }

        [MaxLength(150, ErrorMessage = "جهة الاتصال للطوارئ لا يمكن أن تتجاوز 150 حرف")]
        public string? EmergencyContact { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف للطوارئ غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف للطوارئ لا يمكن أن يتجاوز 30 رقمًا")]
        public string? EmergencyPhone { get; set; }

        // Optional: Password for direct creation
        public string? Password { get; set; }
    }

    public class UpdatePatientDto
    {
        [MaxLength(150, ErrorMessage = "الاسم الكامل لا يمكن أن يتجاوز 150 حرفًا")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string? Phone { get; set; }

        public int? CityId { get; set; }

        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string? Address { get; set; }

        [MaxLength(100, ErrorMessage = "رقم الهوية الوطنية لا يمكن أن يتجاوز 100 حرف")]
        public string? NationalId { get; set; }

        public int? InsuranceCompanyId { get; set; }

        [MaxLength(100, ErrorMessage = "رقم التأمين لا يمكن أن يتجاوز 100 حرف")]
        public string? InsuranceNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }

        [MaxLength(10, ErrorMessage = "الجنس لا يمكن أن يتجاوز 10 أحرف")]
        public string? Gender { get; set; }

        [MaxLength(5, ErrorMessage = "فصيلة الدم لا يمكن أن تتجاوز 5 أحرف")]
        public string? BloodType { get; set; }

        [MaxLength(150, ErrorMessage = "جهة الاتصال للطوارئ لا يمكن أن تتجاوز 150 حرف")]
        public string? EmergencyContact { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف للطوارئ غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف للطوارئ لا يمكن أن يتجاوز 30 رقمًا")]
        public string? EmergencyPhone { get; set; }
    }

    public class PatientSearchDto
    {
        public string? SearchTerm { get; set; }
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int? CityId { get; set; }
        public int? InsuranceCompanyId { get; set; }
        public string? Gender { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}