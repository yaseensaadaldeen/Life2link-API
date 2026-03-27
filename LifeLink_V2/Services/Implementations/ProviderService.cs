using LifeLink_V2.Data;
using LifeLink_V2.DTOs.Provider;
using LifeLink_V2.Helpers;
using LifeLink_V2.Models;
using LifeLink_V2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeLink_V2.Services.Implementations
{
    public class ProviderService : IProviderService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProviderService> _logger;

        public ProviderService(AppDbContext context, ILogger<ProviderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Provider CRUD Methods
        public async Task<ApiResponse> CreateProviderAsync(CreateProviderDto createDto, int createdBy)
        {
            try
            {
                // Validate email doesn't exist
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == createDto.Email.ToLower() && !u.IsDeleted))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "البريد الإلكتروني مسجل مسبقاً",
                        Errors = new List<string> { "Email already exists" }
                    };
                }

                // Validate provider type exists
                var providerType = await _context.ProviderTypes.FindAsync(createDto.ProviderTypeId);
                if (providerType == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "نوع المؤسسة غير صحيح",
                        Errors = new List<string> { "Provider type not found" }
                    };
                }

                // Validate city exists
                var city = await _context.Cities
                    .Include(c => c.Governorate)
                    .FirstOrDefaultAsync(c => c.CityId == createDto.CityId && c.GovernorateId == createDto.GovernorateId);

                if (city == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المدينة أو المحافظة غير صحيحة",
                        Errors = new List<string> { "Invalid city or governorate" }
                    };
                }

                // Validate license number doesn't exist
                if (!string.IsNullOrEmpty(createDto.MedicalLicenseNumber))
                {
                    var existingLicense = await _context.Providers
                        .FirstOrDefaultAsync(p => p.MedicalLicenseNumber == createDto.MedicalLicenseNumber);

                    if (existingLicense != null)
                    {
                        return new ApiResponse
                        {
                            Success = false,
                            Message = "رقم الرخصة الطبية مسجل مسبقاً",
                            Errors = new List<string> { "Medical license number already registered" }
                        };
                    }
                }

                // Get Provider role
                var providerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Provider");
                if (providerRole == null)
                {
                    _logger.LogError("Provider role not found in database");
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "خطأ في النظام، الرجاء المحاولة لاحقاً",
                        Errors = new List<string> { "Provider role not found" }
                    };
                }

                // Create User
                var user = new User
                {
                    FullName = createDto.FullName.Trim(),
                    Email = createDto.Email.ToLower().Trim(),
                    PasswordHash = PasswordHelper.HashPassword(createDto.Password),
                    Phone = createDto.Phone.Trim(),
                    CityId = createDto.CityId,
                    Address = createDto.Address.Trim(),
                    RoleId = providerRole.RoleId,
                    IsActive = true, // Auto-activate for admin creation
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Create Provider
                var provider = new Provider
                {
                    UserId = user.UserId,
                    ProviderTypeId = createDto.ProviderTypeId,
                    ProviderName = createDto.ProviderName.Trim(),
                    CityId = createDto.CityId,
                    Address = createDto.Address.Trim(),
                    Phone = createDto.ProviderPhone?.Trim() ?? createDto.Phone.Trim(),
                    Email = createDto.ProviderEmail?.ToLower().Trim(),
                    IsActive = true, // Auto-activate for admin creation
                    MedicalLicenseNumber = createDto.MedicalLicenseNumber.Trim(),
                    LicenseIssuedBy = createDto.LicenseIssuedBy.Trim(),
                    LicenseExpiry = DateOnly.FromDateTime(createDto.LicenseExpiry),
                    Description = createDto.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _context.Providers.AddAsync(provider);
                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(createdBy, "CreateProvider", "Provider", provider.ProviderId.ToString(),
                    $"Provider created: {provider.ProviderName} (Type: {providerType.ProviderTypeName})");

                var providerInfo = new
                {
                    ProviderId = provider.ProviderId,
                    UserId = user.UserId,
                    ProviderName = provider.ProviderName,
                    OwnerName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    ProviderType = providerType.ProviderTypeName,
                    Message = "تم إنشاء المؤسسة الطبية بنجاح"
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم إنشاء المؤسسة الطبية بنجاح",
                    Data = providerInfo
                };
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating provider");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ في قاعدة البيانات",
                    Errors = new List<string> { "Database error occurred" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating provider");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء إنشاء المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetProviderByIdAsync(int providerId)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.User)
                        .ThenInclude(u => u!.City)
                            .ThenInclude(c => c!.Governorate)
                    .Include(p => p.ProviderType)
                    .Include(p => p.ProviderDoctors.Where(d => d.IsActive))
                        .ThenInclude(d => d.Specialty)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted);

                if (provider == null || provider.User == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                // Get statistics
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);

                var totalAppointments = await _context.Appointments
                    .CountAsync(a => a.ProviderId == providerId && !a.IsDeleted);

                var todayAppointments = await _context.Appointments
                    .CountAsync(a => a.ProviderId == providerId &&
                                    a.ScheduledAt.Date == today &&
                                    !a.IsDeleted);

                var monthAppointments = await _context.Appointments
                    .CountAsync(a => a.ProviderId == providerId &&
                                    a.ScheduledAt >= monthStart &&
                                    !a.IsDeleted);

                var pendingAppointments = await _context.Appointments
                    .CountAsync(a => a.ProviderId == providerId &&
                                    a.Status.StatusName == "Pending" &&
                                    !a.IsDeleted);

                var totalDoctors = await _context.ProviderDoctors
                    .CountAsync(d => d.ProviderId == providerId && d.IsActive);

                var providerDto = new ProviderDto
                {
                    ProviderId = provider.ProviderId,
                    UserId = provider.UserId,
                    ProviderName = provider.ProviderName,
                    OwnerName = provider.User.FullName,
                    OwnerEmail = provider.User.Email,
                    OwnerPhone = provider.User.Phone,
                    ProviderTypeId = provider.ProviderTypeId,
                    ProviderType = provider.ProviderType?.ProviderTypeName ?? string.Empty,
                    CityId = provider.CityId,
                    CityName = provider.User.City?.CityName,
                    GovernorateName = provider.User.City?.Governorate?.GovernorateName,
                    Address = provider.Address,
                    Phone = provider.Phone,
                    Email = provider.Email,
                    Rating = provider.Rating,
                    TotalAppointments = totalAppointments,
                    TotalDoctors = totalDoctors,
                    MedicalLicenseNumber = provider.MedicalLicenseNumber,
                    LicenseIssuedBy = provider.LicenseIssuedBy,
                    LicenseExpiry = Convert.ToDateTime(provider.LicenseExpiry),
                    IsActive = provider.IsActive,
                    CreatedAt = provider.CreatedAt,
                    Description = provider.Description,
                    TodayAppointments = todayAppointments,
                    ThisMonthAppointments = monthAppointments,
                    PendingAppointments = pendingAppointments
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب بيانات المؤسسة بنجاح",
                    Data = providerDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب بيانات المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetProviderByUserIdAsync(int userId)
        {
            try
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

                if (provider == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                return await GetProviderByIdAsync(provider.ProviderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by user ID: {UserId}", userId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب بيانات المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> UpdateProviderAsync(int providerId, UpdateProviderDto updateDto, int updatedBy)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted);

                if (provider == null || provider.User == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                // Update Provider info
                if (!string.IsNullOrWhiteSpace(updateDto.ProviderName))
                    provider.ProviderName = updateDto.ProviderName.Trim();

                if (!string.IsNullOrWhiteSpace(updateDto.Phone))
                    provider.Phone = updateDto.Phone.Trim();

                if (!string.IsNullOrWhiteSpace(updateDto.Email))
                    provider.Email = updateDto.Email.Trim();

                if (updateDto.CityId.HasValue)
                {
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
                    provider.CityId = updateDto.CityId.Value;
                    provider.User.CityId = updateDto.CityId.Value;
                }

                if (updateDto.Address != null)
                {
                    provider.Address = updateDto.Address.Trim();
                    provider.User.Address = updateDto.Address.Trim();
                }

                if (!string.IsNullOrWhiteSpace(updateDto.MedicalLicenseNumber))
                {
                    // Check if new license number is unique
                    if (updateDto.MedicalLicenseNumber != provider.MedicalLicenseNumber &&
                        await _context.Providers.AnyAsync(p => p.MedicalLicenseNumber == updateDto.MedicalLicenseNumber))
                    {
                        return new ApiResponse
                        {
                            Success = false,
                            Message = "رقم الرخصة الطبية مسجل مسبقاً",
                            Errors = new List<string> { "Medical license number already exists" }
                        };
                    }
                    provider.MedicalLicenseNumber = updateDto.MedicalLicenseNumber.Trim();
                }

                if (!string.IsNullOrWhiteSpace(updateDto.LicenseIssuedBy))
                    provider.LicenseIssuedBy = updateDto.LicenseIssuedBy.Trim();

                if (updateDto.LicenseExpiry.HasValue)
                    provider.LicenseExpiry =DateOnly.FromDateTime( updateDto.LicenseExpiry.Value);

                if (updateDto.Description != null)
                    provider.Description = updateDto.Description.Trim();

                if (updateDto.IsActive.HasValue)
                {
                    provider.IsActive = updateDto.IsActive.Value;
                    provider.User.IsActive = updateDto.IsActive.Value;
                }

                provider.User.UpdatedAt = DateTime.UtcNow;
                provider.User.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(updatedBy, "UpdateProvider", "Provider", providerId.ToString(),
                    $"Provider updated: {provider.ProviderName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم تحديث بيانات المؤسسة بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث بيانات المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> DeleteProviderAsync(int providerId, int deletedBy)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted);

                if (provider == null || provider.User == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                // Soft delete provider and user
                provider.IsDeleted = true;
                provider.DeletedAt = DateTime.UtcNow;
                provider.DeletedBy = deletedBy;

                provider.User.IsDeleted = true;
                provider.User.DeletedAt = DateTime.UtcNow;
                provider.User.DeletedBy = deletedBy;

                // Also mark related records as deleted
                var appointments = await _context.Appointments
                    .Where(a => a.ProviderId == providerId && !a.IsDeleted)
                    .ToListAsync();

                foreach (var appointment in appointments)
                {
                    appointment.IsDeleted = true;
                    appointment.DeletedAt = DateTime.UtcNow;
                    appointment.DeletedBy = deletedBy;
                }

                var doctors = await _context.ProviderDoctors
                    .Where(d => d.ProviderId == providerId)
                    .ToListAsync();

                foreach (var doctor in doctors)
                {
                    doctor.IsActive = false;
                }

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(deletedBy, "DeleteProvider", "Provider", providerId.ToString(),
                    $"Provider deleted: {provider.ProviderName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم حذف المؤسسة بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء حذف المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> SearchProvidersAsync(ProviderSearchDto searchDto, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Providers
                    .Include(p => p.User)
                        .ThenInclude(u => u!.City)
                            .ThenInclude(c => c!.Governorate)
                    .Include(p => p.ProviderType)
                    .Where(p => !p.IsDeleted);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    var searchTerm = searchDto.SearchTerm.Trim();
                    query = query.Where(p =>
                        p.ProviderName.Contains(searchTerm) ||
                        p.User!.FullName.Contains(searchTerm) ||
                        p.Email!.Contains(searchTerm) ||
                        p.Phone!.Contains(searchTerm) ||
                        p.MedicalLicenseNumber!.Contains(searchTerm));
                }

                if (searchDto.ProviderTypeId.HasValue)
                    query = query.Where(p => p.ProviderTypeId == searchDto.ProviderTypeId.Value);

                if (searchDto.CityId.HasValue)
                    query = query.Where(p => p.CityId == searchDto.CityId.Value);

                if (searchDto.IsActive.HasValue)
                    query = query.Where(p => p.IsActive == searchDto.IsActive.Value);

                if (searchDto.MinRating.HasValue)
                    query = query.Where(p => p.Rating >= searchDto.MinRating.Value);

                if (!string.IsNullOrWhiteSpace(searchDto.MedicalLicenseNumber))
                    query = query.Where(p => p.MedicalLicenseNumber == searchDto.MedicalLicenseNumber);

                if (searchDto.HasAvailableDoctors.HasValue && searchDto.HasAvailableDoctors.Value)
                {
                    query = query.Where(p => p.ProviderDoctors.Any(d => d.IsActive));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var providers = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProviderDto
                    {
                        ProviderId = p.ProviderId,
                        UserId = p.UserId,
                        ProviderName = p.ProviderName,
                        OwnerName = p.User!.FullName,
                        OwnerEmail = p.User.Email,
                        OwnerPhone = p.User.Phone,
                        ProviderTypeId = p.ProviderTypeId,
                        ProviderType = p.ProviderType!.ProviderTypeName,
                        CityId = p.CityId,
                        CityName = p.User.City != null ? p.User.City.CityName : null,
                        GovernorateName = p.User.City != null && p.User.City.Governorate != null
                            ? p.User.City.Governorate.GovernorateName
                            : null,
                        Address = p.Address,
                        Phone = p.Phone,
                        Email = p.Email,
                        Rating = p.Rating,
                        MedicalLicenseNumber = p.MedicalLicenseNumber,
                        LicenseIssuedBy = p.LicenseIssuedBy,
                        LicenseExpiry = Convert.ToDateTime( p.LicenseExpiry),
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        Description = p.Description,
                        TotalDoctors = p.ProviderDoctors.Count(d => d.IsActive)
                    })
                    .ToListAsync();

                // Get appointment counts for each provider
                foreach (var provider in providers)
                {
                    provider.TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == provider.ProviderId && !a.IsDeleted);
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب قائمة المؤسسات بنجاح",
                    Data = new
                    {
                        Providers = providers,
                        Pagination = new
                        {
                            Page = page,
                            PageSize = pageSize,
                            TotalCount = totalCount,
                            TotalPages = totalPages,
                            HasPrevious = page > 1,
                            HasNext = page < totalPages
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching providers");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء البحث عن المؤسسات",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetProviderStatsAsync(int providerId)
        {
            try
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted);

                if (provider == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                var stats = new
                {
                    TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == providerId && !a.IsDeleted),

                    CompletedAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == providerId && a.Status.StatusName == "Completed" && !a.IsDeleted),

                    TodayAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == providerId &&
                                        a.ScheduledAt.Date == today &&
                                        !a.IsDeleted),

                    ThisMonthAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == providerId &&
                                        a.ScheduledAt >= monthStart &&
                                        !a.IsDeleted),

                    ThisYearAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == providerId &&
                                        a.ScheduledAt >= yearStart &&
                                        !a.IsDeleted),

                    PendingAppointments = await _context.Appointments
                        .CountAsync(a => a.ProviderId == providerId &&
                                        a.Status.StatusName == "Pending" &&
                                        !a.IsDeleted),

                    TotalDoctors = await _context.ProviderDoctors
                        .CountAsync(d => d.ProviderId == providerId && d.IsActive),

                    ActiveDoctors = await _context.ProviderDoctors
                        .CountAsync(d => d.ProviderId == providerId && d.IsActive),

                    // Pharmacy stats (if provider is a pharmacy)
                    TotalMedicines = await _context.Medicines
                        .CountAsync(m => m.ProviderId == providerId && !m.IsDeleted),

                    LowStockMedicines = await _context.Medicines
                        .CountAsync(m => m.ProviderId == providerId &&
                                        m.QuantityInStock <= m.LowStockThreshold &&
                                        !m.IsDeleted),

                    // Lab stats (if provider is a lab)
                    TotalLabTests = await _context.LabTests
                        .CountAsync(l => l.ProviderId == providerId),

                    TotalLabOrders = await _context.LabTestOrders
                        .CountAsync(o => o.ProviderId == providerId),

                    LastAppointment = await _context.Appointments
                        .Where(a => a.ProviderId == providerId && !a.IsDeleted)
                        .OrderByDescending(a => a.ScheduledAt)
                        .Select(a => a.ScheduledAt)
                        .FirstOrDefaultAsync()
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب إحصائيات المؤسسة بنجاح",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider stats for ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب إحصائيات المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetRecentProvidersAsync(int count = 10)
        {
            try
            {
                var recentProviders = await _context.Providers
                    .Include(p => p.User)
                        .ThenInclude(u => u!.City)
                            .ThenInclude(c => c!.Governorate)
                    .Include(p => p.ProviderType)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .Select(p => new
                    {
                        p.ProviderId,
                        p.ProviderName,
                        p.User!.FullName,
                        p.User.Email,
                        p.Phone,
                        ProviderType = p.ProviderType!.ProviderTypeName,
                        City = p.User.City != null ? p.User.City.CityName : null,
                        Governorate = p.User.City != null && p.User.City.Governorate != null
                            ? p.User.City.Governorate.GovernorateName
                            : null,
                        p.IsActive,
                        p.CreatedAt,
                        DoctorCount = p.ProviderDoctors.Count(d => d.IsActive),
                        AppointmentCount = _context.Appointments.Count(a => a.ProviderId == p.ProviderId && !a.IsDeleted)
                    })
                    .ToListAsync();

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب أحدث المؤسسات بنجاح",
                    Data = recentProviders
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent providers");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب أحدث المؤسسات",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> UpdateProviderStatusAsync(int providerId, bool isActive, int updatedBy)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted);

                if (provider == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                provider.IsActive = isActive;
                provider.User!.IsActive = isActive;
                provider.User.UpdatedAt = DateTime.UtcNow;
                provider.User.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(updatedBy, "UpdateProviderStatus", "Provider", providerId.ToString(),
                    $"Provider status updated to {(isActive ? "Active" : "Inactive")}");

                return new ApiResponse
                {
                    Success = true,
                    Message = $"تم {(isActive ? "تفعيل" : "تعطيل")} المؤسسة بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider status for ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث حالة المؤسسة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // Doctor Management Methods
        public async Task<ApiResponse> AddDoctorAsync(int providerId, CreateDoctorDto createDto, int createdBy)
        {
            try
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted && p.IsActive);

                if (provider == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة أو غير نشطة",
                        Errors = new List<string> { "Provider not found or inactive" }
                    };
                }

                // Validate specialty exists if provided
                if (createDto.SpecialtyId.HasValue)
                {
                    var specialty = await _context.MedicalSpecialties.FindAsync(createDto.SpecialtyId.Value);
                    if (specialty == null)
                    {
                        return new ApiResponse
                        {
                            Success = false,
                            Message = "التخصص غير موجود",
                            Errors = new List<string> { "Specialty not found" }
                        };
                    }
                }

                // Create doctor
                var doctor = new ProviderDoctor
                {
                    ProviderId = providerId,
                    FullName = createDto.FullName.Trim(),
                    SpecialtyId = createDto.SpecialtyId,
                    Phone = createDto.Phone?.Trim(),
                    Email = createDto.Email?.ToLower().Trim(),
                    WorkingHours = createDto.WorkingHours?.Trim(),
                    MedicalLicenseNumber = createDto.MedicalLicenseNumber?.Trim(),
                    LicenseExpiry = DateOnly.FromDateTime((DateTime)createDto.LicenseExpiry),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _context.ProviderDoctors.AddAsync(doctor);
                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(createdBy, "AddDoctor", "ProviderDoctor", doctor.DoctorId.ToString(),
                    $"Doctor added: {doctor.FullName} to provider: {provider.ProviderName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم إضافة الدكتور بنجاح",
                    Data = new { DoctorId = doctor.DoctorId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding doctor to provider ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء إضافة الدكتور",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetDoctorByIdAsync(int doctorId)
        {
            try
            {
                var doctor = await _context.ProviderDoctors
                    .Include(d => d.Provider)
                        .ThenInclude(p => p!.User)
                    .Include(d => d.Specialty)
                    .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

                if (doctor == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "الدكتور غير موجود",
                        Errors = new List<string> { "Doctor not found" }
                    };
                }

                // Get statistics
                var today = DateTime.Today;

                var totalAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctorId && !a.IsDeleted);

                var todayAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctorId &&
                                    a.ScheduledAt.Date == today &&
                                    !a.IsDeleted);

                var doctorDto = new ProviderDoctorDto
                {
                    DoctorId = doctor.DoctorId,
                    ProviderId = doctor.ProviderId,
                    FullName = doctor.FullName,
                    SpecialtyId = doctor.SpecialtyId,
                    SpecialtyName = doctor.Specialty?.SpecialtyName,
                    Phone = doctor.Phone,
                    Email = doctor.Email,
                    WorkingHours = doctor.WorkingHours,
                    MedicalLicenseNumber = doctor.MedicalLicenseNumber,
                    LicenseExpiry =Convert.ToDateTime( doctor.LicenseExpiry),
                    IsActive = doctor.IsActive,
                    CreatedAt = doctor.CreatedAt,
                    TotalAppointments = totalAppointments,
                    TodayAppointments = todayAppointments
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب بيانات الدكتور بنجاح",
                    Data = doctorDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor by ID: {DoctorId}", doctorId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب بيانات الدكتور",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetDoctorsByProviderAsync(int providerId, bool? activeOnly = true)
        {
            try
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && !p.IsDeleted);

                if (provider == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المؤسسة غير موجودة",
                        Errors = new List<string> { "Provider not found" }
                    };
                }

                var query = _context.ProviderDoctors
                    .Include(d => d.Specialty)
                    .Where(d => d.ProviderId == providerId);

                if (activeOnly.HasValue && activeOnly.Value)
                    query = query.Where(d => d.IsActive);

                var doctors = await query
                    .OrderBy(d => d.FullName)
                    .Select(d => new ProviderDoctorDto
                    {
                        DoctorId = d.DoctorId,
                        ProviderId = d.ProviderId,
                        FullName = d.FullName,
                        SpecialtyId = d.SpecialtyId,
                        SpecialtyName = d.Specialty != null ? d.Specialty.SpecialtyName : null,
                        Phone = d.Phone,
                        Email = d.Email,
                        WorkingHours = d.WorkingHours,
                        MedicalLicenseNumber = d.MedicalLicenseNumber,
                        LicenseExpiry = Convert.ToDateTime(d.LicenseExpiry)     ,
                        IsActive = d.IsActive,
                        CreatedAt = d.CreatedAt
                    })
                    .ToListAsync();

                // Get appointment counts for each doctor
                foreach (var doctor in doctors)
                {
                    doctor.TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.DoctorId && !a.IsDeleted);

                    doctor.TodayAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.DoctorId &&
                                        a.ScheduledAt.Date == DateTime.Today &&
                                        !a.IsDeleted);
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جبل قائمة الأطباء بنجاح",
                    Data = doctors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctors for provider ID: {ProviderId}", providerId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب قائمة الأطباء",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDto, int updatedBy)
        {
            try
            {
                var doctor = await _context.ProviderDoctors
                    .Include(d => d.Provider)
                    .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

                if (doctor == null || doctor.Provider == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "الدكتور غير موجود",
                        Errors = new List<string> { "Doctor not found" }
                    };
                }

                // Update doctor info
                if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                    doctor.FullName = updateDto.FullName.Trim();

                if (updateDto.SpecialtyId.HasValue)
                {
                    var specialty = await _context.MedicalSpecialties.FindAsync(updateDto.SpecialtyId.Value);
                    if (specialty == null)
                    {
                        return new ApiResponse
                        {
                            Success = false,
                            Message = "التخصص غير موجود",
                            Errors = new List<string> { "Specialty not found" }
                        };
                    }
                    doctor.SpecialtyId = updateDto.SpecialtyId.Value;
                }

                if (updateDto.Phone != null)
                    doctor.Phone = updateDto.Phone.Trim();

                if (updateDto.Email != null)
                    doctor.Email = updateDto.Email.Trim();

                if (updateDto.WorkingHours != null)
                    doctor.WorkingHours = updateDto.WorkingHours.Trim();

                if (updateDto.MedicalLicenseNumber != null)
                    doctor.MedicalLicenseNumber = updateDto.MedicalLicenseNumber.Trim();

                if (updateDto.LicenseExpiry.HasValue)
                    doctor.LicenseExpiry =DateOnly.FromDateTime( updateDto.LicenseExpiry.Value);

                if (updateDto.IsActive.HasValue)
                    doctor.IsActive = updateDto.IsActive.Value;

                doctor.CreatedBy = updatedBy; // Track who made the update

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(updatedBy, "UpdateDoctor", "ProviderDoctor", doctorId.ToString(),
                    $"Doctor updated: {doctor.FullName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم تحديث بيانات الدكتور بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor ID: {DoctorId}", doctorId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث بيانات الدكتور",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> DeleteDoctorAsync(int doctorId, int deletedBy)
        {
            try
            {
                var doctor = await _context.ProviderDoctors
                    .Include(d => d.Provider)
                    .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

                if (doctor == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "الدكتور غير موجود",
                        Errors = new List<string> { "Doctor not found" }
                    };
                }

                // Soft delete - just mark as inactive
                doctor.IsActive = false;

                // Also cancel any future appointments with this doctor
                var futureAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId &&
                               a.ScheduledAt > DateTime.UtcNow &&
                               (a.Status.StatusName == "Pending" || a.Status.StatusName == "Confirmed") &&
                               !a.IsDeleted)
                    .ToListAsync();

                foreach (var appointment in futureAppointments)
                {
                    appointment.Status.StatusName = "Cancelled";
                    appointment.CancelReason = "تم إلغاء الموعد بسبب تعطيل الدكتور";
                    appointment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(deletedBy, "DeleteDoctor", "ProviderDoctor", doctorId.ToString(),
                    $"Doctor deactivated: {doctor.FullName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم تعطيل الدكتور بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting doctor ID: {DoctorId}", doctorId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تعطيل الدكتور",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetDoctorStatsAsync(int doctorId)
        {
            try
            {
                var doctor = await _context.ProviderDoctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "الدكتور غير موجود",
                        Errors = new List<string> { "Doctor not found" }
                    };
                }

                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                var stats = new
                {
                    TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId && !a.IsDeleted),

                    CompletedAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId && a.Status.StatusName == "Completed" && !a.IsDeleted),

                    TodayAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId &&
                                        a.ScheduledAt.Date == today &&
                                        !a.IsDeleted),

                    ThisMonthAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId &&
                                        a.ScheduledAt >= monthStart &&
                                        !a.IsDeleted),

                    ThisYearAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId &&
                                        a.ScheduledAt >= yearStart &&
                                        !a.IsDeleted),

                    PendingAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId &&
                                        a.Status.StatusName == "Pending" &&
                                        !a.IsDeleted),

                    CancelledAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId &&
                                        a.Status.StatusName == "Cancelled" &&
                                        !a.IsDeleted),

                    NoShowAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctorId &&
                                        a.Status.StatusName == "NoShow" &&
                                        !a.IsDeleted),

                    LastAppointment = await _context.Appointments
                        .Where(a => a.DoctorId == doctorId && !a.IsDeleted)
                        .OrderByDescending(a => a.ScheduledAt)
                        .Select(a => a.ScheduledAt)
                        .FirstOrDefaultAsync(),

                    BusiestDay = await _context.Appointments
                        .Where(a => a.DoctorId == doctorId && !a.IsDeleted)
                        .GroupBy(a => a.ScheduledAt.Date)
                        .OrderByDescending(g => g.Count())
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .FirstOrDefaultAsync()
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب إحصائيات الدكتور بنجاح",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor stats for ID: {DoctorId}", doctorId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب إحصائيات الدكتور",
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