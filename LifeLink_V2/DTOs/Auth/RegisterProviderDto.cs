using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Auth
{
    public class RegisterProviderDto
    {
        [Required(ErrorMessage = "اسم المالك/المسؤول مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم لا يمكن أن يتجاوز 150 حرفًا")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(150, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 150 حرفًا")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 8 أحرف")]
        [MaxLength(100, ErrorMessage = "كلمة المرور لا يمكن أن تتجاوز 100 حرف")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير، حرف صغير، رقم، ورمز خاص")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
        [MaxLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 رقمًا")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار محافظة صحيحة")]
        public int GovernorateId { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار مدينة صحيحة")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [MaxLength(300, ErrorMessage = "العنوان لا يمكن أن يتجاوز 300 حرف")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المؤسسة مطلوب")]
        [MaxLength(250, ErrorMessage = "اسم المؤسسة لا يمكن أن يتجاوز 250 حرف")]
        public string ProviderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع المؤسسة مطلوب")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار نوع مؤسسة صحيح")]
        public int ProviderTypeId { get; set; }

        [Required(ErrorMessage = "رقم الرخصة الطبية مطلوب")]
        [MaxLength(150, ErrorMessage = "رقم الرخصة الطبية لا يمكن أن يتجاوز 150 حرف")]
        public string MedicalLicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "جهة إصدار الرخصة مطلوبة")]
        [MaxLength(250, ErrorMessage = "جهة إصدار الرخصة لا يمكن أن تتجاوز 250 حرف")]
        public string LicenseIssuedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ انتهاء الرخصة مطلوب")]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "تاريخ انتهاء الرخصة يجب أن يكون في المستقبل")]
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

    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                if (date <= DateTime.Today)
                {
                    return new ValidationResult("التاريخ يجب أن يكون في المستقبل");
                }
            }
            return ValidationResult.Success;
        }
    }
}