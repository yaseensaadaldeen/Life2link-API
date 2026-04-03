using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LifeLink_V2.DTOs.Auth;
using LifeLink_V2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeLink_V2.Helpers;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register/patient")]
        public async Task<ActionResult<AuthResponseDto>> RegisterPatient(RegisterPatientDto registerDto)
        {
            _logger.LogInformation("Patient registration attempt for email: {Email}", registerDto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Patient registration validation failed: {Errors}", string.Join(", ", errors));

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = errors
                });
            }

            var result = await _authService.RegisterPatientAsync(registerDto);

            if (!result.Success)
            {
                _logger.LogWarning("Patient registration failed: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Patient registration successful for email: {Email}", registerDto.Email);
            return CreatedAtAction(nameof(RegisterPatient), result);
        }

        [HttpPost("register/provider")]
        public async Task<ActionResult<AuthResponseDto>> RegisterProvider(RegisterProviderDto registerDto)
        {
            _logger.LogInformation("Provider registration attempt for email: {Email}", registerDto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Provider registration validation failed: {Errors}", string.Join(", ", errors));

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = errors
                });
            }

            var result = await _authService.RegisterProviderAsync(registerDto);

            if (!result.Success)
            {
                _logger.LogWarning("Provider registration failed: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Provider registration successful for email: {Email}", registerDto.Email);
            return CreatedAtAction(nameof(RegisterProvider), result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Login validation failed: {Errors}", string.Join(", ", errors));

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = errors
                });
            }

            var result = await _authService.LoginAsync(loginDto);

            if (!result.Success)
            {
                _logger.LogWarning("Login failed for email: {Email} - {Message}", loginDto.Email, result.Message);
                return Unauthorized(result);
            }

            _logger.LogInformation("Login successful for email: {Email}", loginDto.Email);
            return Ok(result);
        }

        [HttpPost("check-email")]
        public async Task<ActionResult<ApiResponse>> CheckEmail([FromBody] CheckEmailDto checkEmailDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = new List<string> { "البريد الإلكتروني مطلوب" }
                });
            }

            var exists = await _authService.EmailExistsAsync(checkEmailDto.Email);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = exists ? "البريد الإلكتروني مسجل مسبقاً" : "البريد الإلكتروني متاح",
                Data = new { emailExists = exists }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<AuthResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "البريد الإلكتروني مطلوب",
                    Errors = new List<string> { "الرجاء إدخال البريد الإلكتروني" }
                });
            }

            _logger.LogInformation("Forgot password request for email: {Email}", forgotPasswordDto.Email);

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto.Email);

            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            _logger.LogInformation("Reset password attempt with token");

            var result = await _authService.ResetPasswordAsync(resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<AuthResponseDto>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "غير مصرح",
                    Errors = new List<string> { "معلومات المستخدم غير صحيحة" }
                });
            }

            _logger.LogInformation("Change password request for user ID: {UserId}", userId);

            var result = await _authService.ChangePasswordAsync(
                userId,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse>> GetProfile()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "غير مصرح",
                    Errors = new List<string> { "معلومات المستخدم غير صحيحة" }
                });
            }

            // Here you would typically fetch the user profile from database
            // For now, return basic info from token claims

            var profile = new
            {
                UserId = userId,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value,
                PatientId = User.FindFirst("PatientId")?.Value,
                ProviderId = User.FindFirst("ProviderId")?.Value,
                ProviderName = User.FindFirst("ProviderName")?.Value
            };

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "تم جلب معلومات الملف الشخصي بنجاح",
                Data = profile
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public ActionResult<ApiResponse> Logout()
        {
            // In a stateless JWT system, logout is handled client-side by removing the token
            // You could implement token blacklisting if needed

            _logger.LogInformation("User logged out");

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "تم تسجيل الخروج بنجاح"
            });
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request"
                });
            }

            var result = await _authService.RefreshTokenAsync(request.Token!, request.RefreshToken!);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }
        [HttpPost("verify-email")]
        public async Task<ActionResult<AuthResponseDto>> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var result = await _authService.VerifyEmailAsync(request.Email, request.Code);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}

    public class CheckEmailDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "رمز إعادة التعيين مطلوب")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 6 أحرف")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare("NewPassword", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 6 أحرف")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare("NewPassword", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

  

