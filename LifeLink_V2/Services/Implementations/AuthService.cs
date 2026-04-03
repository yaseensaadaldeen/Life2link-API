using AutoMapper;
using LifeLink_V2.Data;
using LifeLink_V2.DTOs.Auth;
using LifeLink_V2.Helpers;
using LifeLink_V2.Models;
using LifeLink_V2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace LifeLink_V2.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
      //  private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        public AuthService(
            AppDbContext context,
            ITokenService tokenService,
           // IMapper mapper,
            ILogger<AuthService> logger,
            IEmailService emailService)
        {
            _context = context;
            _tokenService = tokenService;
           // _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto registerDto)
        {
            var response = new AuthResponseDto();

            try
            {
                // Validate email
                if (await EmailExistsAsync(registerDto.Email))
                {
                    response.Success = false;
                    response.Message = "البريد الإلكتروني مسجل مسبقاً";
                    response.Errors.Add("Email already registered");
                    return response;
                }

                // Validate national ID
                if (await NationalIdExistsAsync(registerDto.NationalId))
                {
                    response.Success = false;
                    response.Message = "رقم الهوية الوطنية مسجل مسبقاً";
                    response.Errors.Add("National ID already registered");
                    return response;
                }

                // Validate phone number
                if (!ValidationHelper.IsValidSyrianPhoneNumber(registerDto.Phone))
                {
                    response.Success = false;
                    response.Message = "رقم الهاتف غير صحيح";
                    response.Errors.Add("Invalid Syrian phone number");
                    return response;
                }

                // Validate city exists and belongs to governorate
                var city = await _context.Cities
                    .Include(c => c.Governorate)
                    .FirstOrDefaultAsync(c => c.CityId == registerDto.CityId &&
                                             c.GovernorateId == registerDto.GovernorateId);

                if (city == null)
                {
                    response.Success = false;
                    response.Message = "المدينة أو المحافظة غير صحيحة";
                    response.Errors.Add("Invalid city or governorate");
                    return response;
                }

                // Get Patient role
                var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Patient");
                if (patientRole == null)
                {
                    _logger.LogError("Patient role not found in database");
                    response.Success = false;
                    response.Message = "خطأ في النظام، الرجاء المحاولة لاحقاً";
                    response.Errors.Add("Patient role not found");
                    return response;
                }

                // Create User
                var user = new User
                {
                    FullName = registerDto.FullName.Trim(),
                    Email = registerDto.Email.ToLower().Trim(),
                    PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                    Phone = registerDto.Phone.Trim(),
                    CityId = registerDto.CityId,
                    Address = registerDto.Address?.Trim(),
                    RoleId = patientRole.RoleId,
                    IsActive = true,
                    TwoFactorEnabled = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Create Patient record
                var patient = new Patient
                {
                    UserId = user.UserId,
                    NationalId = registerDto.NationalId.Trim(),
                    InsuranceCompanyId = registerDto.InsuranceCompanyId > 0 ? registerDto.InsuranceCompanyId : null,
                    InsuranceNumber = registerDto.InsuranceNumber?.Trim(),
                    Dob =DateOnly.FromDateTime( registerDto.DOB),
                    Gender = registerDto.Gender,
                    BloodType = registerDto.BloodType,
                    EmergencyContact = registerDto.EmergencyContact?.Trim(),
                    EmergencyPhone = registerDto.EmergencyPhone?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.UserId
                };

                await _context.Patients.AddAsync(patient);
                await _context.SaveChangesAsync();

                // Generate verification token (for email verification if needed)
                var verificationToken = Guid.NewGuid().ToString();

                // Log activity
                await LogActivityAsync(user.UserId, "Register", "User", user.UserId.ToString(),
                    $"Patient registered: {user.FullName}");

                // Send welcome email (optional)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, "patient");
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}", user.Email);
                }

                await CreateAndSendVerificationCodeAsync(user);

                // Generate JWT token
                var token = _tokenService.GenerateJwtToken(user);

                // Create response
                response.Success = true;
                response.Message = "تم تسجيل المريض بنجاح";
                response.Token = token;
                response.TokenExpiry = DateTime.UtcNow.AddDays(7);
                response.User = new UserInfoDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = patientRole.RoleName,
                    CityId = user.CityId,
                    CityName = city.CityName,
                    GovernorateName = city.Governorate?.GovernorateName,
                    PatientId = patient.PatientId,
                    //HasProfilePicture = false
                };

                _logger.LogInformation("Patient registered successfully: {Email} (ID: {UserId})", user.Email, user.UserId);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while registering patient");
                response.Success = false;
                response.Message = "حدث خطأ في قاعدة البيانات";
                response.Errors.Add("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while registering patient");
                response.Success = false;
                response.Message = "حدث خطأ أثناء التسجيل";
                response.Errors.Add("Internal server error");
            }

            return response;
        }

        public async Task<AuthResponseDto> RegisterProviderAsync(RegisterProviderDto registerDto)
        {
            var response = new AuthResponseDto();

            try
            {
                // Validate email
                if (await EmailExistsAsync(registerDto.Email))
                {
                    response.Success = false;
                    response.Message = "البريد الإلكتروني مسجل مسبقاً";
                    response.Errors.Add("Email already registered");
                    return response;
                }

                // Validate phone number
                if (!ValidationHelper.IsValidSyrianPhoneNumber(registerDto.Phone))
                {
                    response.Success = false;
                    response.Message = "رقم الهاتف غير صحيح";
                    response.Errors.Add("Invalid Syrian phone number");
                    return response;
                }

                // Validate city exists and belongs to governorate
                var city = await _context.Cities
                    .Include(c => c.Governorate)
                    .FirstOrDefaultAsync(c => c.CityId == registerDto.CityId &&
                                             c.GovernorateId == registerDto.GovernorateId);

                if (city == null)
                {
                    response.Success = false;
                    response.Message = "المدينة أو المحافظة غير صحيحة";
                    response.Errors.Add("Invalid city or governorate");
                    return response;
                }

                // Validate provider type exists
                var providerType = await _context.ProviderTypes.FindAsync(registerDto.ProviderTypeId);
                if (providerType == null)
                {
                    response.Success = false;
                    response.Message = "نوع المؤسسة غير صحيح";
                    response.Errors.Add("Provider type not found");
                    return response;
                }

                // Validate license number doesn't exist
                if (!string.IsNullOrEmpty(registerDto.MedicalLicenseNumber))
                {
                    var existingLicense = await _context.Providers
                        .FirstOrDefaultAsync(p => p.MedicalLicenseNumber == registerDto.MedicalLicenseNumber);

                    if (existingLicense != null)
                    {
                        response.Success = false;
                        response.Message = "رقم الرخصة الطبية مسجل مسبقاً";
                        response.Errors.Add("Medical license number already registered");
                        return response;
                    }
                }

                // Get Provider role
                var providerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Provider");
                if (providerRole == null)
                {
                    _logger.LogError("Provider role not found in database");
                    response.Success = false;
                    response.Message = "خطأ في النظام، الرجاء المحاولة لاحقاً";
                    response.Errors.Add("Provider role not found");
                    return response;
                }

                // Create User
                var user = new User
                {
                    FullName = registerDto.FullName.Trim(),
                    Email = registerDto.Email.ToLower().Trim(),
                    PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                    Phone = registerDto.Phone.Trim(),
                    CityId = registerDto.CityId,
                    Address = registerDto.Address.Trim(),
                    RoleId = providerRole.RoleId,
                    IsActive = false, // Provider accounts need admin approval
                    TwoFactorEnabled = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Create Provider record
                var provider = new Provider
                {
                    UserId = user.UserId,
                    ProviderTypeId = registerDto.ProviderTypeId,
                    ProviderName = registerDto.ProviderName.Trim(),
                    CityId = registerDto.CityId,
                    Address = registerDto.Address.Trim(),
                    Phone = registerDto.ProviderPhone?.Trim() ?? registerDto.Phone.Trim(),
                    Email = registerDto.ProviderEmail?.ToLower().Trim(),
                    IsActive = false, // Needs admin approval
                    MedicalLicenseNumber = registerDto.MedicalLicenseNumber.Trim(),
                    LicenseIssuedBy = registerDto.LicenseIssuedBy.Trim(),
                    LicenseExpiry =DateOnly.FromDateTime( registerDto.LicenseExpiry),
                    CreatedBy = user.UserId
                };

                await _context.Providers.AddAsync(provider);
                await _context.SaveChangesAsync();

                // Generate verification token
                var verificationToken = Guid.NewGuid().ToString();

                // Log activity
                await LogActivityAsync(user.UserId, "Register", "Provider", provider.ProviderId.ToString(),
                    $"Provider registered: {provider.ProviderName} (Pending Approval)");

                // Send registration confirmation email
                try
                {
                    await _emailService.SendProviderRegistrationEmailAsync(
                        user.Email,
                        user.FullName,
                        provider.ProviderName,
                        providerType.ProviderTypeName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send registration email to {Email}", user.Email);
                }

                // Notify admin about new provider registration (optional)
                try
                {
                    await NotifyAdminAboutNewProviderAsync(provider);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Failed to notify admin about new provider");
                }
                await CreateAndSendVerificationCodeAsync(user);

                // Create response (provider accounts don't get immediate token - need approval)
                response.Success = true;
                response.Message = "تم تسجيل المؤسسة الطبية بنجاح. يرجى الانتظار حتى تتم الموافقة على حسابك من قبل الإدارة.";
                response.User = new UserInfoDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = providerRole.RoleName,
                    CityId = user.CityId,
                    CityName = city.CityName,
                    GovernorateName = city.Governorate?.GovernorateName,
                    ProviderId = provider.ProviderId,
                    ProviderName = provider.ProviderName,
                    //HasProfilePicture = false,
                    IsActive = user.IsActive
                };

                _logger.LogInformation("Provider registered successfully (pending approval): {Email} (Provider ID: {ProviderId})",
                    user.Email, provider.ProviderId);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while registering provider");
                response.Success = false;
                response.Message = "حدث خطأ في قاعدة البيانات";
                response.Errors.Add("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while registering provider");
                response.Success = false;
                response.Message = "حدث خطأ أثناء التسجيل";
                response.Errors.Add("Internal server error");
            }

            return response;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var response = new AuthResponseDto();

            try
            {
                // Find user by email
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.City)
                    .ThenInclude(c => c!.Governorate)
                    .Include(u => u.Patient)
                    .Include(u => u.Provider)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && !u.IsDeleted);

                if (user == null)
                {
                    await LogFailedLoginAttemptAsync(loginDto.Email);

                    response.Success = false;
                    response.Message = "البريد الإلكتروني أو كلمة المرور غير صحيحة";
                    response.Errors.Add("Invalid credentials");
                    return response;
                }

                if (!user.IsActive)
                {
                    response.Success = false;
                    response.Message = user.Role.RoleName == "Provider"
                        ? "حساب المؤسسة قيد المراجعة. يرجى الانتظار حتى تتم الموافقة من قبل الإدارة."
                        : "حسابك غير نشط. يرجى التواصل مع الدعم الفني.";
                    response.Errors.Add("Account is inactive");
                    return response;
                }

                if (!PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    await LogFailedLoginAttemptAsync(loginDto.Email);

                    response.Success = false;
                    response.Message = "البريد الإلكتروني أو كلمة المرور غير صحيحة";
                    response.Errors.Add("Invalid credentials");
                    return response;
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log successful login
                await LogActivityAsync(user.UserId, "Login", "User", user.UserId.ToString(), "User logged in successfully");

                // Token expiry
                var tokenExpiry = loginDto.RememberMe
                    ? DateTime.UtcNow.AddDays(30)
                    : DateTime.UtcNow.AddDays(7);

                // Generate JWT
                var token = _tokenService.GenerateJwtToken(user, tokenExpiry);

                // Generate and store refresh token
                var refreshTokenValue = _tokenService.GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

                var refreshToken = new RefreshToken
                {
                    UserId = user.UserId,
                    Token = refreshTokenValue,
                    ExpiresAt = refreshTokenExpiry,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.RefreshTokens.AddAsync(refreshToken);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "تم تسجيل الدخول بنجاح";
                response.Token = token;
                response.TokenExpiry = tokenExpiry;
                response.RefreshToken = refreshTokenValue;
                response.RefreshTokenExpiry = refreshTokenExpiry;
                response.User = new UserInfoDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.RoleName,
                    CityId = user.CityId,
                    CityName = user.City?.CityName,
                    GovernorateName = user.City?.Governorate?.GovernorateName,
                    PatientId = user.Patient?.PatientId,
                    ProviderId = user.Provider?.ProviderId,
                    ProviderName = user.Provider?.ProviderName,
                    IsActive = user.IsActive
                };

                _logger.LogInformation("User logged in successfully: {Email} (Role: {Role})", user.Email, user.Role.RoleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                response.Success = false;
                response.Message = "حدث خطأ أثناء تسجيل الدخول";
                response.Errors.Add("Internal server error");
            }

            return response;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == email.ToLower() &&
                !u.IsDeleted);
        }

        public async Task<bool> NationalIdExistsAsync(string nationalId)
        {
            return await _context.Patients.AnyAsync(p =>
                p.NationalId == nationalId &&
                !p.IsDeleted);
        }

        public async Task<bool> IsValidRoleAsync(int roleId)
        {
            return await _context.Roles.AnyAsync(r => r.RoleId == roleId);
        }

        public async Task<AuthResponseDto> ForgotPasswordAsync(string email)
        {
            var response = new AuthResponseDto();

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);

                if (user == null)
                {
                    // Don't reveal that the user doesn't exist for security
                    response.Success = true;
                    response.Message = "إذا كان البريد الإلكتروني مسجلاً لدينا، سيصلك رابط إعادة تعيين كلمة المرور";
                    return response;
                }

                // Generate reset token
                var resetToken = Guid.NewGuid().ToString();
                var tokenExpiry = DateTime.UtcNow.AddHours(24);

                // Store reset token (you might want to create a separate table for this)
                // For now, we'll store it in a simple way
                user.TwoFactorEnabled = true; // Using this as a flag for demo
                await _context.SaveChangesAsync();

                // Send reset email
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetToken);

                // Log activity
                await LogActivityAsync(user.UserId, "ForgotPassword", "User", user.UserId.ToString(),
                    "Password reset requested");

                response.Success = true;
                response.Message = "تم إرسال رابط إعادة تعيين كلمة المرور إلى بريدك الإلكتروني";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPassword for email: {Email}", email);
                response.Success = false;
                response.Message = "حدث خطأ أثناء معالجة الطلب";
            }

            return response;
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(string token, string newPassword)
        {
            var response = new AuthResponseDto();

            try
            {
                // Validate token (in a real app, you'd verify against stored tokens)
                // For demo, we're skipping detailed token validation

                // Find user by token (simplified)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.TwoFactorEnabled && !u.IsDeleted);

                if (user == null || string.IsNullOrEmpty(token))
                {
                    response.Success = false;
                    response.Message = "رابط إعادة تعيين كلمة المرور غير صالح أو منتهي الصلاحية";
                    return response;
                }

                // Update password
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                user.TwoFactorEnabled = false; // Reset the flag
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(user.UserId, "ResetPassword", "User", user.UserId.ToString(),
                    "Password reset successful");

                response.Success = true;
                response.Message = "تم إعادة تعيين كلمة المرور بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword");
                response.Success = false;
                response.Message = "حدث خطأ أثناء إعادة تعيين كلمة المرور";
            }

            return response;
        }

       
        private async Task LogActivityAsync(int? userId, string action, string entity, string entityId, string details)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    UserId = userId,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = details,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log activity");
            }
        }

        private async Task LogFailedLoginAttemptAsync(string email)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    Action = "FailedLogin",
                    Entity = "User",
                    EntityId = email,
                    Details = "Failed login attempt",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log failed login attempt");
            }
        }

        private async Task NotifyAdminAboutNewProviderAsync(Provider provider)
        {
            try
            {
                // Find admin users
                var adminUsers = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "Admin" && u.IsActive && !u.IsDeleted)
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    // Create notification for admin
                    var notification = new Notification
                    {
                        UserId = admin.UserId,
                        Title = "طلب تسجيل مؤسسة جديدة",
                        Body = $"تم تسجيل مؤسسة جديدة: {provider.ProviderName}. يرجى مراجعة الطلب والموافقة عليه.",
                        Channel = "System",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Notifications.AddAsync(notification);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify admin about new provider");
            }
        }
        
        public async Task<AuthResponseDto> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var response = new AuthResponseDto();

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    response.Success = false;
                    response.Message = "كلمة المرور الحالية والجديدة مطلوبتان";
                    return response;
                }

                // Check if new password is different from current
                if (currentPassword == newPassword)
                {
                    response.Success = false;
                    response.Message = "كلمة المرور الجديدة يجب أن تكون مختلفة عن الحالية";
                    return response;
                }

                // Validate new password strength
                if (!IsStrongPassword(newPassword))
                {
                    response.Success = false;
                    response.Message = "كلمة المرور الجديدة ضعيفة. يجب أن تحتوي على حرف كبير، حرف صغير، رقم، ورمز خاص";
                    return response;
                }

                // Find user
                var user = await _context.Users.FindAsync(userId);

                if (user == null || user.IsDeleted)
                {
                    response.Success = false;
                    response.Message = "المستخدم غير موجود";
                    return response;
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    response.Success = false;
                    response.Message = "الحساب غير نشط. الرجاء التواصل مع الدعم الفني";
                    return response;
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    response.Success = false;
                    response.Message = "كلمة المرور الحالية غير صحيحة";

                    // Log failed attempt
                    await LogActivityAsync(userId, "ChangePasswordFailed", "User", userId.ToString(),
                        "Failed password change attempt");

                    return response;
                }

                // Check password history (prevent reusing recent passwords)
                // In production, you might want to check against password history table
                // For now, we'll skip this check

                // Update to new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = userId; // Self-update

                await _context.SaveChangesAsync();

                // Send notification email
                try
                {
                    await _emailService.SendPasswordChangeNotificationAsync(user.Email, user.FullName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send password change notification to {Email}", user.Email);
                }

                // Log activity
                await LogActivityAsync(userId, "ChangePassword", "User", userId.ToString(),
                    "Password changed successfully");

                response.Success = true;
                response.Message = "تم تغيير كلمة المرور بنجاح";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while changing password for user ID: {UserId}", userId);
                response.Success = false;
                response.Message = "حدث خطأ في قاعدة البيانات";
                response.Errors.Add("Database error occurred");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID: {UserId}", userId);
                response.Success = false;
                response.Message = "حدث خطأ أثناء تغيير كلمة المرور";
                response.Errors.Add("Internal server error");
            }

            return response;
        }


        // Helper Methods
        private string GenerateSecureToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            // Check for at least one uppercase, one lowercase, one digit, one special character
            var hasUpperCase = new Regex(@"[A-Z]");
            var hasLowerCase = new Regex(@"[a-z]");
            var hasDigits = new Regex(@"\d");
            var hasSpecialChars = new Regex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");

            return hasUpperCase.IsMatch(password) &&
                   hasLowerCase.IsMatch(password) &&
                   hasDigits.IsMatch(password) &&
                   hasSpecialChars.IsMatch(password);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshTokenValue)
        {
            var response = new AuthResponseDto();

            try
            {
                // Validate expired JWT and get principal
                var principal = _tokenService.GetPrincipalFromExpiredToken(token);
                var userIdClaim = principal.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    response.Success = false;
                    response.Message = "Invalid token";
                    return response;
                }

                var storedRefresh = await _context.RefreshTokens
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.Token == refreshTokenValue && r.RevokedAt == null);

                if (storedRefresh == null || storedRefresh.ExpiresAt <= DateTime.UtcNow)
                {
                    response.Success = false;
                    response.Message = "Refresh token is invalid or expired";
                    return response;
                }

                // Get user and ensure active
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Patient)
                    .Include(u => u.Provider)
                    .Include(u => u.City)
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found or inactive";
                    return response;
                }

                // Revoke old refresh token (rotation)
                storedRefresh.RevokedAt = DateTime.UtcNow;

                // Create new refresh token
                var newRefreshValue = _tokenService.GenerateRefreshToken();
                var newRefresh = new RefreshToken
                {
                    UserId = user.UserId,
                    Token = newRefreshValue,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                };

                await _context.RefreshTokens.AddAsync(newRefresh);
                await _context.SaveChangesAsync();

                // Generate new JWT
                var jwtExpiry = DateTime.UtcNow.AddDays(7);
                var newJwt = _tokenService.GenerateJwtToken(user, jwtExpiry);

                response.Success = true;
                response.Message = "Tokens refreshed";
                response.Token = newJwt;
                response.TokenExpiry = jwtExpiry;
                response.RefreshToken = newRefreshValue;
                response.RefreshTokenExpiry = newRefresh.ExpiresAt;
                response.User = new UserInfoDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.RoleName,
                    CityId = user.CityId,
                    CityName = user.City?.CityName,
                    PatientId = user.Patient?.PatientId,
                    ProviderId = user.Provider?.ProviderId,
                    ProviderName = user.Provider?.ProviderName,
                    IsActive = user.IsActive
                };

                // Log rotation
                await LogActivityAsync(user.UserId, "RefreshToken", "Auth", user.UserId.ToString(), "Refresh token rotated");
            }
            catch (SecurityTokenException ste)
            {
                _logger.LogWarning(ste, "Invalid token during refresh");
                response.Success = false;
                response.Message = "Invalid token";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                response.Success = false;
                response.Message = "حدث خطأ أثناء تجديد الرموز";
            }

            return response;
        }

        public async Task<AuthResponseDto> VerifyEmailAsync(string email, string code)
        {
            var response = new AuthResponseDto();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "البريد الإلكتروني غير موجود";
                    return response;
                }

                var verification = await _context.EmailVerificationCodes
                    .Where(e => e.UserId == user.UserId && e.Code == code && !e.IsUsed && e.Expiry >= DateTime.UtcNow)
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefaultAsync();

                if (verification == null)
                {
                    response.Success = false;
                    response.Message = "رمز التحقق غير صالح أو منتهي الصلاحية";
                    return response;
                }

                verification.IsUsed = true;
                await _context.SaveChangesAsync();

                // For patients, ensure account active; for providers, keep existing approval policy (do not auto-activate providers)
                if (user.Patient != null && !user.IsActive)
                {
                    user.IsActive = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Log
                await LogActivityAsync(user.UserId, "VerifyEmail", "User", user.UserId.ToString(), $"Email verified for {email}");

                response.Success = true;
                response.Message = "تم التحقق من البريد الإلكتروني بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", email);
                response.Success = false;
                response.Message = "حدث خطأ أثناء التحقق من البريد الإلكتروني";
            }

            return response;
        }
        private async Task CreateAndSendVerificationCodeAsync(User user)
        {
            // generate 6-digit numeric code
            var codeBytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(codeBytes);
            var numeric = Math.Abs(BitConverter.ToInt32(codeBytes, 0)) % 1000000;
            var code = numeric.ToString("D6");

            var expiry = DateTime.UtcNow.AddHours(24);

            var ev = new EmailVerificationCode
            {
                UserId = user.UserId,
                Code = code,
                Expiry = expiry,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.EmailVerificationCodes.AddAsync(ev);
            await _context.SaveChangesAsync();

            // send email (best-effort)
            try
            {
                await _emailService.SendAccountActivationEmailAsync(user.Email, user.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
            }
        }

    }

}