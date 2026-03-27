using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Auth
{
    public class RegisterPatientDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم الكامل لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 6 أحرف")]
        [MaxLength(100, ErrorMessage = "كلمة المرور لا يمكن أن تتجاوز 100 حرف")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير، حرف صغير، رقم، ورمز خاص")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{10,}$", ErrorMessage = "أدخل رقم هاتف صحيح")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار محافظة صحيحة")]
        public int GovernorateId { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار مدينة صحيحة")]
        public int CityId { get; set; }

        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "رقم الهوية الوطنية مطلوب")]
        [MaxLength(100, ErrorMessage = "رقم الهوية الوطنية لا يمكن أن يتجاوز 100 حرف")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "رقم الهوية الوطنية يجب أن يحتوي على أرقام فقط")]
        public string NationalId { get; set; } = string.Empty;

        public int? InsuranceCompanyId { get; set; }

        [MaxLength(100, ErrorMessage = "رقم التأمين لا يمكن أن يتجاوز 100 حرف")]
        public string? InsuranceNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [DataType(DataType.Date)]
        [MinimumAge(18, ErrorMessage = "يجب أن يكون عمرك 18 سنة على الأقل")]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [MaxLength(10, ErrorMessage = "الجنس لا يمكن أن يتجاوز 10 أحرف")]
        [RegularExpression("^(ذكر|أنثى)$", ErrorMessage = "الجنس يجب أن يكون 'ذكر' أو 'أنثى'")]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(5, ErrorMessage = "فصيلة الدم لا يمكن أن تتجاوز 5 أحرف")]
        [RegularExpression("^(A\\+|A-|B\\+|B-|AB\\+|AB-|O\\+|O-)$", ErrorMessage = "فصيلة الدم غير صحيحة")]
        public string? BloodType { get; set; }

        [MaxLength(150, ErrorMessage = "جهة الاتصال للطوارئ لا يمكن أن تتجاوز 150 حرف")]
        public string? EmergencyContact { get; set; }

        [Phone(ErrorMessage = "صيغة رقم الهاتف للطوارئ غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف للطوارئ لا يمكن أن يتجاوز 30 رقمًا")]
        public string? EmergencyPhone { get; set; }
    }

    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var age = DateTime.Today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

                if (age < _minimumAge)
                {
                    return new ValidationResult($"يجب أن يكون عمرك {_minimumAge} سنة على الأقل");
                }
            }
            return ValidationResult.Success;
        }
    }
}