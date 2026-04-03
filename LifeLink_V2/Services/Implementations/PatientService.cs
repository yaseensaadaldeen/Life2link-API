using LifeLink_V2.Data;
using LifeLink_V2.DTOs.Patient;
using LifeLink_V2.DTOs.Discovery;
using LifeLink_V2.Helpers;
using LifeLink_V2.Models;
using LifeLink_V2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeLink_V2.Services.Implementations
{
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PatientService> _logger;

        public PatientService(AppDbContext context, ILogger<PatientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Patient CRUD & search (existing)

        public async Task<ApiResponse> CreatePatientAsync(CreatePatientDto createDto, int createdBy)
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

                // Validate national ID doesn't exist
                if (await _context.Patients.AnyAsync(p => p.NationalId == createDto.NationalId && !p.IsDeleted))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "رقم الهوية الوطنية مسجل مسبقاً",
                        Errors = new List<string> { "National ID already exists" }
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

                // Get Patient role
                var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Patient");
                if (patientRole == null)
                {
                    _logger.LogError("Patient role not found in database");
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "خطأ في النظام، الرجاء المحاولة لاحقاً",
                        Errors = new List<string> { "Patient role not found" }
                    };
                }

                // Generate password if not provided
                var password = !string.IsNullOrEmpty(createDto.Password)
                    ? createDto.Password
                    : PasswordHelper.GenerateRandomPassword();

                // Create User
                var user = new User
                {
                    FullName = createDto.FullName.Trim(),
                    Email = createDto.Email.ToLower().Trim(),
                    PasswordHash = PasswordHelper.HashPassword(password),
                    Phone = createDto.Phone.Trim(),
                    CityId = createDto.CityId,
                    Address = createDto.Address?.Trim(),
                    RoleId = patientRole.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Create Patient
                var patient = new Patient
                {
                    UserId = user.UserId,
                    NationalId = createDto.NationalId.Trim(),
                    InsuranceCompanyId = createDto.InsuranceCompanyId,
                    InsuranceNumber = createDto.InsuranceNumber?.Trim(),
                    Dob = DateOnly.FromDateTime(createDto.DOB),
                    Gender = createDto.Gender,
                    BloodType = createDto.BloodType,
                    EmergencyContact = createDto.EmergencyContact?.Trim(),
                    EmergencyPhone = createDto.EmergencyPhone?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _context.Patients.AddAsync(patient);
                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(createdBy, "CreatePatient", "Patient", patient.PatientId.ToString(),
                    $"Patient created: {user.FullName} (ID: {patient.PatientId})");

                var patientInfo = new
                {
                    PatientId = patient.PatientId,
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    GeneratedPassword = string.IsNullOrEmpty(createDto.Password) ? password : null,
                    Message = string.IsNullOrEmpty(createDto.Password)
                        ? "تم إنشاء المريض بنجاح. تم إنشاء كلمة مرور مؤقتة."
                        : "تم إنشاء المريض بنجاح."
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم إنشاء المريض بنجاح",
                    Data = patientInfo
                };
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating patient");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ في قاعدة البيانات",
                    Errors = new List<string> { "Database error occurred" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating patient");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء إنشاء المريض",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetPatientByIdAsync(int patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                        .ThenInclude(u => u!.City)
                            .ThenInclude(c => c!.Governorate)
                    .Include(p => p.InsuranceCompany)
                    .Include(p => p.MedicalRecords)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

                if (patient == null || patient.User == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المريض غير موجود",
                        Errors = new List<string> { "Patient not found" }
                    };
                }

                // Get appointment and order counts
                var appointmentCount = await _context.Appointments
                    .CountAsync(a => a.PatientId == patientId && !a.IsDeleted);

                var orderCount = await _context.PharmacyOrders
                    .CountAsync(o => o.PatientId == patientId && !o.IsDeleted) +
                    await _context.LabTestOrders
                    .CountAsync(o => o.PatientId == patientId);

                var patientDto = new PatientDto
                {
                    PatientId = patient.PatientId,
                    UserId = patient.UserId,
                    FullName = patient.User.FullName,
                    Email = patient.User.Email,
                    Phone = patient.User.Phone,
                    CityId = patient.User.CityId,
                    CityName = patient.User.City?.CityName,
                    GovernorateName = patient.User.City?.Governorate?.GovernorateName,
                    NationalId = patient.NationalId,
                    InsuranceCompanyId = patient.InsuranceCompanyId,
                    InsuranceCompanyName = patient.InsuranceCompany?.CompanyName,
                    InsuranceNumber = patient.InsuranceNumber,
                    DOB = patient.Dob.HasValue ? patient.Dob.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    Gender = patient.Gender,
                    BloodType = patient.BloodType,
                    EmergencyContact = patient.EmergencyContact,
                    EmergencyPhone = patient.EmergencyPhone,
                    CreatedAt = patient.CreatedAt,
                    IsActive = patient.User.IsActive,
                    TotalAppointments = appointmentCount,
                    TotalOrders = orderCount
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب بيانات المريض بنجاح",
                    Data = patientDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient by ID: {PatientId}", patientId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب بيانات المريض",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetPatientByUserIdAsync(int userId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

                if (patient == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المريض غير موجود",
                        Errors = new List<string> { "Patient not found" }
                    };
                }

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient by user ID: {UserId}", userId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب بيانات المريض",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> UpdatePatientAsync(int patientId, UpdatePatientDto updateDto, int updatedBy)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

                if (patient == null || patient.User == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المريض غير موجود",
                        Errors = new List<string> { "Patient not found" }
                    };
                }

                // Update User info
                if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                    patient.User.FullName = updateDto.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(updateDto.Phone))
                    patient.User.Phone = updateDto.Phone.Trim();

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
                    patient.User.CityId = updateDto.CityId.Value;
                }

                if (updateDto.Address != null)
                    patient.User.Address = updateDto.Address.Trim();

                // Update Patient info
                if (!string.IsNullOrWhiteSpace(updateDto.NationalId))
                {
                    // Check if new national ID is unique
                    if (updateDto.NationalId != patient.NationalId &&
                        await _context.Patients.AnyAsync(p => p.NationalId == updateDto.NationalId && !p.IsDeleted))
                    {
                        return new ApiResponse
                        {
                            Success = false,
                            Message = "رقم الهوية الوطنية مسجل مسبقاً",
                            Errors = new List<string> { "National ID already exists" }
                        };
                    }
                    patient.NationalId = updateDto.NationalId.Trim();
                }

                if (updateDto.InsuranceCompanyId.HasValue)
                    patient.InsuranceCompanyId = updateDto.InsuranceCompanyId.Value;

                if (!string.IsNullOrWhiteSpace(updateDto.InsuranceNumber))
                    patient.InsuranceNumber = updateDto.InsuranceNumber.Trim();

                if (updateDto.DOB.HasValue)
                    patient.Dob = DateOnly.FromDateTime(updateDto.DOB.Value);

                if (!string.IsNullOrWhiteSpace(updateDto.Gender))
                    patient.Gender = updateDto.Gender;

                if (updateDto.BloodType != null)
                    patient.BloodType = updateDto.BloodType;

                if (updateDto.EmergencyContact != null)
                    patient.EmergencyContact = updateDto.EmergencyContact.Trim();

                if (updateDto.EmergencyPhone != null)
                    patient.EmergencyPhone = updateDto.EmergencyPhone.Trim();

                patient.User.UpdatedAt = DateTime.UtcNow;
                patient.User.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(updatedBy, "UpdatePatient", "Patient", patientId.ToString(),
                    $"Patient updated: {patient.User.FullName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم تحديث بيانات المريض بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient ID: {PatientId}", patientId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث بيانات المريض",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> DeletePatientAsync(int patientId, int deletedBy)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

                if (patient == null || patient.User == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المريض غير موجود",
                        Errors = new List<string> { "Patient not found" }
                    };
                }

                // Soft delete patient and user
                patient.IsDeleted = true;
                patient.DeletedAt = DateTime.UtcNow;
                patient.DeletedBy = deletedBy;

                patient.User.IsDeleted = true;
                patient.User.DeletedAt = DateTime.UtcNow;
                patient.User.DeletedBy = deletedBy;

                // Also mark appointments and orders as deleted
                var appointments = await _context.Appointments
                    .Where(a => a.PatientId == patientId && !a.IsDeleted)
                    .ToListAsync();

                foreach (var appointment in appointments)
                {
                    appointment.IsDeleted = true;
                    appointment.DeletedAt = DateTime.UtcNow;
                    appointment.DeletedBy = deletedBy;
                }

                var pharmacyOrders = await _context.PharmacyOrders
                    .Where(o => o.PatientId == patientId && !o.IsDeleted)
                    .ToListAsync();

                foreach (var order in pharmacyOrders)
                {
                    order.IsDeleted = true;
                    order.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Log activity
                await LogActivityAsync(deletedBy, "DeletePatient", "Patient", patientId.ToString(),
                    $"Patient deleted: {patient.User.FullName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم حذف المريض بنجاح"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient ID: {PatientId}", patientId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء حذف المريض",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> SearchPatientsAsync(PatientSearchDto searchDto, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Patients
                    .Include(p => p.User)
                        .ThenInclude(u => u!.City)
                            .ThenInclude(c => c!.Governorate)
                    .Include(p => p.InsuranceCompany)
                    .Where(p => !p.IsDeleted);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    var searchTerm = searchDto.SearchTerm.Trim();
                    query = query.Where(p =>
                        p.User!.FullName.Contains(searchTerm) ||
                        p.User.Email.Contains(searchTerm) ||
                        p.NationalId.Contains(searchTerm) ||
                        p.User.Phone!.Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(searchDto.NationalId))
                    query = query.Where(p => p.NationalId == searchDto.NationalId);

                if (!string.IsNullOrWhiteSpace(searchDto.Phone))
                    query = query.Where(p => p.User!.Phone!.Contains(searchDto.Phone));

                if (!string.IsNullOrWhiteSpace(searchDto.Email))
                    query = query.Where(p => p.User!.Email.Contains(searchDto.Email));

                if (searchDto.CityId.HasValue)
                    query = query.Where(p => p.User!.CityId == searchDto.CityId);

                if (searchDto.InsuranceCompanyId.HasValue)
                    query = query.Where(p => p.InsuranceCompanyId == searchDto.InsuranceCompanyId);

                if (!string.IsNullOrWhiteSpace(searchDto.Gender))
                    query = query.Where(p => p.Gender == searchDto.Gender);

                if (searchDto.IsActive.HasValue)
                    query = query.Where(p => p.User!.IsActive == searchDto.IsActive.Value);

                if (searchDto.CreatedFrom.HasValue)
                    query = query.Where(p => p.CreatedAt >= searchDto.CreatedFrom.Value);

                if (searchDto.CreatedTo.HasValue)
                    query = query.Where(p => p.CreatedAt <= searchDto.CreatedTo.Value);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var patients = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PatientDto
                    {
                        PatientId = p.PatientId,
                        UserId = p.UserId,
                        FullName = p.User!.FullName,
                        Email = p.User.Email,
                        Phone = p.User.Phone,
                        CityId = p.User.CityId,
                        CityName = p.User.City != null ? p.User.City.CityName : null,
                        GovernorateName = p.User.City != null && p.User.City.Governorate != null
                            ? p.User.City.Governorate.GovernorateName
                            : null,
                        NationalId = p.NationalId,
                        InsuranceCompanyId = p.InsuranceCompanyId,
                        InsuranceCompanyName = p.InsuranceCompany != null ? p.InsuranceCompany.CompanyName : null,
                        InsuranceNumber = p.InsuranceNumber,
                        DOB = Convert.ToDateTime(p.Dob),
                        Gender = p.Gender,
                        BloodType = p.BloodType,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.User.IsActive
                    })
                    .ToListAsync();

                // Get counts for each patient
                foreach (var patient in patients)
                {
                    patient.TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patient.PatientId && !a.IsDeleted);

                    patient.TotalOrders = await _context.PharmacyOrders
                        .CountAsync(o => o.PatientId == patient.PatientId && !o.IsDeleted) +
                        await _context.LabTestOrders
                        .CountAsync(o => o.PatientId == patient.PatientId);
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب قائمة المرضى بنجاح",
                    Data = new
                    {
                        Patients = patients,
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
                _logger.LogError(ex, "Error searching patients");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء البحث عن المرضى",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetPatientStatsAsync(int patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

                if (patient == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "المريض غير موجود",
                        Errors = new List<string> { "Patient not found" }
                    };
                }

                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                var stats = new
                {
                    TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId && !a.IsDeleted),

                    CompletedAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId && a.Status.StatusName == "Completed" && !a.IsDeleted),

                    UpcomingAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId &&
                                        a.ScheduledAt > DateTime.UtcNow &&
                                        a.Status.StatusName == "Confirmed" &&
                                        !a.IsDeleted),

                    TotalPharmacyOrders = await _context.PharmacyOrders
                        .CountAsync(o => o.PatientId == patientId && !o.IsDeleted),

                    TotalLabOrders = await _context.LabTestOrders
                        .CountAsync(o => o.PatientId == patientId),

                    MonthlyAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId &&
                                        a.CreatedAt >= monthStart &&
                                        !a.IsDeleted),

                    YearlyAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId &&
                                        a.CreatedAt >= yearStart &&
                                        !a.IsDeleted),

                    LastAppointment = await _context.Appointments
                        .Where(a => a.PatientId == patientId && !a.IsDeleted)
                        .OrderByDescending(a => a.ScheduledAt)
                        .Select(a => a.ScheduledAt)
                        .FirstOrDefaultAsync(),

                    LastOrder = await _context.PharmacyOrders
                        .Where(o => o.PatientId == patientId && !o.IsDeleted)
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => o.CreatedAt)
                        .FirstOrDefaultAsync()
                };

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب إحصائيات المريض بنجاح",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient stats for ID: {PatientId}", patientId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب إحصائيات المريض",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse> GetRecentPatientsAsync(int count = 10)
        {
            try
            {
                var recentPatients = await _context.Patients
                    .Include(p => p.User)
                        .ThenInclude(u => u!.City)
                            .ThenInclude(c => c!.Governorate)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .Select(p => new
                    {
                        p.PatientId,
                        p.User!.FullName,
                        p.User.Email,
                        p.User.Phone,
                        p.NationalId,
                        p.Gender,
                        p.Dob,
                        City = p.User.City != null ? p.User.City.CityName : null,
                        Governorate = p.User.City != null && p.User.City.Governorate != null
                            ? p.User.City.Governorate.GovernorateName
                            : null,
                        p.CreatedAt,
                        IsActive = p.User.IsActive,
                        AppointmentCount = _context.Appointments.Count(a => a.PatientId == p.PatientId && !a.IsDeleted)
                    })
                    .ToListAsync();

                return new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب أحدث المرضى بنجاح",
                    Data = recentPatients
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent patients");
                return new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب أحدث المرضى",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion

        #region IPatientService implementations (required)

        public async Task<ApiResponse> GetDashboardAsync(int userId)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
                if (patient == null) return ApiResponseHelper.NotFound("المريض غير موجود");

                // cancelled status id (safe fallback)
                var cancelledId = await _context.AppointmentStatuses
                    .Where(s => s.StatusName == "Cancelled")
                    .Select(s => s.StatusId)
                    .FirstOrDefaultAsync();

                var upcoming = await _context.Appointments
                    .CountAsync(a => a.PatientId == patient.PatientId && a.ScheduledAt >= DateTime.UtcNow && a.StatusId != cancelledId);

                var pendingPrescriptions = await _context.Prescriptions
                    .Where(p => p.PatientId == patient.PatientId && p.Status != null && p.Status != "Completed")
                    .CountAsync();

                var unreadNotifications = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

                var outstanding = await _context.Payments.Where(p => p.PatientId == patient.PatientId )
                    .SumAsync(p => (decimal?)p.AmountSyp) ?? 0m;

                var dto = new PatientDashboardDto
                {
                    UpcomingAppointments = upcoming,
                    PendingPrescriptions = pendingPrescriptions,
                    UnreadNotifications = unreadNotifications,
                    OutstandingPaymentsSyp = outstanding
                };

                return ApiResponseHelper.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building patient dashboard for user {UserId}", userId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> SearchDiscoveryAsync(string type, string query, int page, int pageSize)
        {
            // implemented above in SearchPatientsAsync style — reuse that logic
            return await SearchDiscoveryInternal(type, query, page, pageSize);
        }

        private async Task<ApiResponse> SearchDiscoveryInternal(string type, string query, int page, int pageSize)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                if (string.IsNullOrWhiteSpace(query))
                    query = string.Empty;

                if (type?.ToLower() == "clinic")
                {
                    var q = _context.Providers.Where(p => p.IsActive && !p.IsDeleted &&
                        (EF.Functions.Like(p.ProviderName, $"%{query}%") || EF.Functions.Like(p.Address ?? "", $"%{query}%")));

                    var total = await q.CountAsync();
                    var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                        .Select(p => new ClinicDto
                        {
                            ProviderId = p.ProviderId,
                            ProviderName = p.ProviderName,
                            Address = p.Address,
                            Phone = p.Phone,
                            CityId = p.CityId,
                            CityName = p.City != null ? p.City.CityName : null,
                            IsActive = p.IsActive
                        }).ToListAsync();

                    return ApiResponseHelper.Success(new { Items = items, Total = total, Page = page, PageSize = pageSize });
                }
                else // doctors
                {
                    var q = _context.ProviderDoctors.Where(d => d.IsActive &&
                        (EF.Functions.Like(d.FullName, $"%{query}%") || EF.Functions.Like(d.WorkingHours ?? "", $"%{query}%")));

                    var total = await q.CountAsync();
                    var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                        .Select(d => new DoctorDto
                        {
                            DoctorId = d.DoctorId,
                            ProviderId = d.ProviderId,
                            FullName = d.FullName,
                            SpecialtyId = d.SpecialtyId,
                            SpecialtyName = d.Specialty != null ? d.Specialty.SpecialtyName : null,
                            Phone = d.Phone,
                            Email = d.Email,
                            WorkingHours = d.WorkingHours,
                            IsActive = d.IsActive
                        }).ToListAsync();

                    return ApiResponseHelper.Success(new { Items = items, Total = total, Page = page, PageSize = pageSize });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching discovery type {Type}", type);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetClinicByIdAsync(int clinicId)
        {
            try
            {
                var p = await _context.Providers
                    .Include(x => x.City)
                    .ThenInclude(c => c.Governorate)
                    .FirstOrDefaultAsync(x => x.ProviderId == clinicId && !x.IsDeleted);

                if (p == null) return ApiResponseHelper.NotFound("Clinic not found");

                var dto = new ClinicDto
                {
                    ProviderId = p.ProviderId,
                    ProviderName = p.ProviderName,
                    Address = p.Address,
                    Phone = p.Phone,
                    CityId = p.CityId,
                    CityName = p.City?.CityName,
                    IsActive = p.IsActive
                };

                return ApiResponseHelper.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching clinic {ClinicId}", clinicId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetDoctorByIdAsync(int doctorId)
        {
            try
            {
                var d = await _context.ProviderDoctors
                    .Include(x => x.Specialty)
                    .Include(x => x.Provider)
                    .FirstOrDefaultAsync(x => x.DoctorId == doctorId && !x.IsActive);

                if (d == null) return ApiResponseHelper.NotFound("Doctor not found");

                var dto = new DoctorDto
                {
                    DoctorId = d.DoctorId,
                    ProviderId = d.ProviderId,
                    FullName = d.FullName,
                    SpecialtyId = d.SpecialtyId,
                    SpecialtyName = d.Specialty?.SpecialtyName,
                    Phone = d.Phone,
                    Email = d.Email,
                    WorkingHours = d.WorkingHours,
                    IsActive = d.IsActive
                };

                return ApiResponseHelper.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching doctor {DoctorId}", doctorId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetClinicSlotsAsync(int clinicId, DateTime date, int? doctorId)
        {
            try
            {
                var appointmentService = new AppointmentService(_context, NullLogger<AppointmentService>.Instance);
                return await appointmentService.GetProviderAvailabilityAsync(clinicId, doctorId, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinic slots for {ClinicId}", clinicId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetDoctorSlotsAsync(int doctorId, DateTime date)
        {
            try
            {
                var appointmentService = new AppointmentService(_context, NullLogger<AppointmentService>.Instance);
                return await appointmentService.GetDoctorAvailabilitySlotsAsync(doctorId, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor slots for {DoctorId}", doctorId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetHealthRecordsOverviewAsync(int userId)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
                if (patient == null) return ApiResponseHelper.NotFound("Patient not found");

                var prescriptionsCount = await _context.Prescriptions.CountAsync(p => p.PatientId == patient.PatientId);
                var labOrdersCount = await _context.LabTestOrders.CountAsync(o => o.PatientId == patient.PatientId);
                var medFilesCount = await _context.MedFiles.CountAsync(m => m.PatientId == patient.PatientId);

                var data = new
                {
                    Prescriptions = prescriptionsCount,
                    LabOrders = labOrdersCount,
                    MedicalFiles = medFilesCount
                };

                return ApiResponseHelper.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health overview for user {UserId}", userId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetPrescriptionsAsync(int userId, string? status, int page, int pageSize)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
                if (patient == null) return ApiResponseHelper.NotFound("Patient not found");

                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = _context.Prescriptions.Where(p => p.PatientId == patient.PatientId && !p.IsDeleted);

                if (!string.IsNullOrWhiteSpace(status))
                    q = q.Where(p => p.Status == status);

                var total = await q.CountAsync();
                var list = await q.OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(p => new PrescriptionSummaryDto
                    {
                        PrescriptionId = p.PrescriptionId,
                        PrescriptionCode = p.PrescriptionCode,
                        CreatedAt = p.CreatedAt,
                        Status = p.Status ?? "Unknown",
                       
                    }).ToListAsync();

                return ApiResponseHelper.Success(new { Items = list, Total = total, Page = page, PageSize = pageSize });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing prescriptions for user {UserId}", userId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetPrescriptionByIdAsync(int userId, int prescriptionId)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
                if (patient == null) return ApiResponseHelper.NotFound("Patient not found");

                var pres = await _context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId && p.PatientId == patient.PatientId);

                if (pres == null) return ApiResponseHelper.NotFound("Prescription not found");

                var dto = new
                {
                    pres.PrescriptionId,
                    pres.PrescriptionCode,
                    pres.CreatedAt,
                    pres.Status,
                    Items = pres.PrescriptionItems.Select(i => new { i.PrescriptionItemId , i.Quantity, i.UnitPriceSyp, i.LineTotalSyp })
                };

                return ApiResponseHelper.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prescription {PrescriptionId}", prescriptionId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetLabResultsAsync(int userId, int limit, int page)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
                if (patient == null) return ApiResponseHelper.NotFound("Patient not found");

                limit = Math.Clamp(limit, 1, 100);
                page = Math.Max(1, page);

                var q = _context.LabTestOrders.Where(o => o.PatientId == patient.PatientId )
                    .OrderByDescending(o => o.CreatedAt);

                var total = await q.CountAsync();
                var items = await q.Skip((page - 1) * limit).Take(limit)
                    .Select(o => new LabResultDto
                    {
                        LabOrderId = o.LabTestOrderId,
                        OrderCode = o.OrderCode,
                        CreatedAt = o.CreatedAt,
                        Status = o.Status ?? "Unknown",
                        Results = null
                    }).ToListAsync();

                return ApiResponseHelper.Success(new { Items = items, Total = total, Page = page, PageSize = limit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing lab results for user {UserId}", userId);
                return ApiResponseHelper.InternalError();
            }
        }

        public async Task<ApiResponse> GetFilesAsync(int userId, string? type, int page, int pageSize)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
                if (patient == null) return ApiResponseHelper.NotFound("Patient not found");

                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = _context.MedFiles.Where(f => f.PatientId == patient.PatientId && !f.IsDeleted);

                if (!string.IsNullOrWhiteSpace(type))
                    q = q.Where(f => f.ContentType != null && f.ContentType.StartsWith(type));

                var total = await q.CountAsync();
                var items = await q.OrderByDescending(f => f.UploadedAt)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(f => new { f.MedFileId, f.MedFileName, f.ContentType, f.UploadedAt })
                    .ToListAsync();

                return ApiResponseHelper.Success(new { Items = items, Total = total, Page = page, PageSize = pageSize });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files for user {UserId}", userId);
                return ApiResponseHelper.InternalError();
            }
        }

        #endregion

        #region Helpers

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

        #endregion
    }
}