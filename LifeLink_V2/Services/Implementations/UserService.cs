using LifeLink_V2.Data;
using LifeLink_V2.DTOs.User;
using LifeLink_V2.Helpers;
using LifeLink_V2.Models;
using LifeLink_V2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeLink_V2.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.City)
                        .ThenInclude(c => c!.Governorate)
                    .Include(u => u.Patient)
                    .Include(u => u.Provider)
                        .ThenInclude(p => p!.ProviderType)
                    .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

                if (user == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المستخدم غير موجود",
                        Errors = new List<string> { "User not found" }
                    };
                }

                var profile = new UserProfileDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    CityId = user.CityId,
                    Address = user.Address,
                    Role = user.Role.RoleName,
                    CityName = user.City?.CityName,
                    GovernorateName = user.City?.Governorate?.GovernorateName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                };

                // Add patient info if exists
                if (user.Patient != null)
                {
                    profile.PatientId = user.Patient.PatientId;
                    profile.NationalId = user.Patient.NationalId;
                    profile.InsuranceNumber = user.Patient.InsuranceNumber;
                    profile.DOB = Convert.ToDateTime(user.Patient.Dob);
                    profile.Gender = user.Patient.Gender;
                    profile.BloodType = user.Patient.BloodType;
                }

                // Add provider info if exists
                if (user.Provider != null)
                {
                    profile.ProviderId = user.Provider.ProviderId;
                    profile.ProviderName = user.Provider.ProviderName;
                    profile.ProviderType = user.Provider.ProviderType?.ProviderTypeName;
                    profile.MedicalLicenseNumber = user.Provider.MedicalLicenseNumber;
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب الملف الشخصي بنجاح",
                    Data = profile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user ID: {UserId}", userId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب الملف الشخصي",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> UpdateUserProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.City)
                    .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

                if (user == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المستخدم غير موجود",
                        Errors = new List<string> { "User not found" }
                    };
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                    user.FullName = updateDto.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(updateDto.Phone))
                    user.Phone = updateDto.Phone.Trim();

                if (updateDto.CityId.HasValue)
                {
                    // Validate city exists
                    var city = await _context.Cities.FindAsync(updateDto.CityId.Value);
                    if (city == null)
                    {
                        return new ApiResponse
                        {
                            Success = false,
                            Message = "المدينة غير موجودة",
                            Errors = new List<string> { "City not found" }
                        };
                    }
                    user.CityId = updateDto.CityId.Value;
                }

                if (updateDto.Address != null)
                    user.Address = updateDto.Address.Trim();

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(userId, "UpdateProfile", "User", userId.ToString(),
                    $"User profile updated: {user.FullName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم تحديث الملف الشخصي بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user ID: {UserId}", userId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث الملف الشخصي",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Patient)
                    .Include(u => u.Provider)
                    .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

                if (user == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    };
                }

                var userInfo = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    user.IsActive,
                    user.CreatedAt,
                    Role = user.Role.RoleName,
                    HasPatient = user.Patient != null,
                    HasProvider = user.Provider != null
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب بيانات المستخدم بنجاح",
                    Data = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب بيانات المستخدم",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetUsersByRoleAsync(string roleName, int page = 1, int pageSize = 20)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "الدور غير موجود",
                        Errors = new List<string> { "Role not found" }
                    };
                }

                var query = _context.Users
                    .Include(u => u.City)
                    .Include(u => u.Patient)
                    .Include(u => u.Provider)
                    .Where(u => u.RoleId == role.RoleId && !u.IsDeleted)
                    .OrderByDescending(u => u.CreatedAt);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.Phone,
                        u.IsActive,
                        u.CreatedAt,
                        u.LastLoginAt,
                        City = u.City != null ? u.City.CityName : null,
                        PatientId = u.Patient != null ? u.Patient.PatientId : (int?)null,
                        ProviderId = u.Provider != null ? u.Provider.ProviderId : (int?)null,
                        ProviderName = u.Provider != null ? u.Provider.ProviderName : null
                    })
                    .ToListAsync();

                return new ApiResponse
                {
                    Success = true,
                    Message = $"تم جلب المستخدمين (دور: {roleName}) بنجاح",
                    Data = new
                    {
                        Users = users,
                        Pagination = new
                        {
                            Page = page,
                            PageSize = pageSize,
                            TotalCount = totalCount,
                            TotalPages = totalPages
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {RoleName}", roleName);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب المستخدمين",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    };
                }

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(userId, "UpdateUserStatus", "User", userId.ToString(),
                    $"User status updated to {(isActive ? "Active" : "Inactive")}");

                return new ApiResponse
                {
                    Success = true,
                    Message = $"تم {(isActive ? "تفعيل" : "تعطيل")} حساب المستخدم بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user ID: {UserId}", userId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث حالة المستخدم",
                    Errors = new List<string> { ex.Message }
                };
            }
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
    }
}