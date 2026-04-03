using LifeLink_V2.Data;
using LifeLink_V2.DTOs.Provider;
using LifeLink_V2.Helpers;
using LifeLink_V2.Models;
using LifeLink_V2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeLink_V2.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private ILogger<PatientService> logger;

        public AppointmentService(AppDbContext context, ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse> GetAppointmentsAsync(int? patientId = null, int? providerId = null,
            int? statusId = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Provider)
                        .ThenInclude(p => p.ProviderType)
                    .Include(a => a.Status)
                    .Include(a => a.Doctor)
                    .Include(a => a.Specialty)
                    .AsQueryable();

                // Apply filters
                if (patientId.HasValue)
                    query = query.Where(a => a.PatientId == patientId.Value);

                if (providerId.HasValue)
                    query = query.Where(a => a.ProviderId == providerId.Value);

                if (statusId.HasValue)
                    query = query.Where(a => a.StatusId == statusId.Value);

                if (dateFrom.HasValue)
                    query = query.Where(a => a.ScheduledAt >= dateFrom.Value);

                if (dateTo.HasValue)
                    query = query.Where(a => a.ScheduledAt <= dateTo.Value);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var appointments = await query
                    .OrderByDescending(a => a.ScheduledAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        a.AppointmentId,
                        a.AppointmentCode,
                        Patient = new
                        {
                            a.Patient.PatientId,
                            a.Patient.User.FullName,
                            a.Patient.User.Phone,
                            a.Patient.NationalId
                        },
                        Provider = new
                        {
                            a.Provider.ProviderId,
                            a.Provider.ProviderName,
                            ProviderType = a.Provider.ProviderType.ProviderTypeName,
                            a.Provider.Phone
                        },
                        Doctor = a.Doctor != null ? new
                        {
                            a.Doctor.DoctorId,
                            a.Doctor.FullName,
                            Specialty = a.Doctor.Specialty != null ? a.Doctor.Specialty.SpecialtyName : a.Specialty.SpecialtyName
                        } : null,
                        Specialty = a.Specialty != null ? a.Specialty.SpecialtyName : (a.Doctor != null && a.Doctor.Specialty != null ? a.Doctor.Specialty.SpecialtyName : null),
                        a.ScheduledAt,
                        a.DurationMinutes,
                        Status = a.Status.StatusName,
                        a.PriceSyp,
                        a.PriceUsd,
                        a.IsPaid,
                        a.BookingSource,
                        a.CreatedAt
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    Appointments = appointments,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع المواعيد");
            }
        }

        public async Task<ApiResponse> GetAppointmentByIdAsync(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Provider)
                        .ThenInclude(p => p.ProviderType)
                    .Include(a => a.Status)
                    .Include(a => a.Doctor)
                    .Include(a => a.Specialty)
                    .Include(a => a.AppointmentMedFiles)
                        .ThenInclude(amf => amf.MedFile)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                    return ApiResponseHelper.NotFound("الموعد غير موجود");

                var result = new
                {
                    appointment.AppointmentId,
                    appointment.AppointmentCode,
                    Patient = new
                    {
                        appointment.Patient.PatientId,
                        appointment.Patient.User.FullName,
                        appointment.Patient.User.Email,
                        appointment.Patient.User.Phone,
                        appointment.Patient.NationalId,
                        appointment.Patient.BloodType,
                        appointment.Patient.Dob
                    },
                    Provider = new
                    {
                        appointment.Provider.ProviderId,
                        appointment.Provider.ProviderName,
                        ProviderType = appointment.Provider.ProviderType.ProviderTypeName,
                        appointment.Provider.Address,
                        appointment.Provider.Phone,
                        appointment.Provider.Email
                    },
                    Doctor = appointment.Doctor != null ? new
                    {
                        appointment.Doctor.DoctorId,
                        appointment.Doctor.FullName,
                        appointment.Doctor.Phone,
                        appointment.Doctor.Email,
                        Specialty = appointment.Doctor.Specialty != null ? appointment.Doctor.Specialty.SpecialtyName : null,
                        appointment.Doctor.WorkingHours
                    } : null,
                    Specialty = appointment.Specialty != null ? appointment.Specialty.SpecialtyName : null,
                    appointment.ScheduledAt,
                    appointment.DurationMinutes,
                    Status = appointment.Status.StatusName,
                    appointment.PriceSyp,
                    appointment.PriceUsd,
                    appointment.ExchangeRate,
                    appointment.IsPaid,
                    appointment.BookingSource,
                    //appointment.Notes,
                    appointment.CancelReason,
                    appointment.CreatedAt,
                    appointment.UpdatedAt,
                    MedFiles = appointment.AppointmentMedFiles.Select(amf => new
                    {
                        amf.MedFile.MedFileId,
                        amf.MedFile.MedFileName,
                        amf.MedFile.ContentType,
                        amf.MedFile.UploadedAt
                    }).ToList()
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment with ID {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع بيانات الموعد");
            }
        }

        public async Task<ApiResponse> CreateAppointmentAsync(CreateAppointmentDto appointmentDto, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate patient exists
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == appointmentDto.PatientId);

                if (patient == null)
                    return ApiResponseHelper.NotFound("المريض غير موجود");

                // Validate provider exists
                var provider = await _context.Providers
                    .Include(p => p.ProviderType)
                    .FirstOrDefaultAsync(p => p.ProviderId == appointmentDto.ProviderId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                // Validate doctor if specified
                if (appointmentDto.DoctorId.HasValue)
                {
                    var doctor = await _context.ProviderDoctors
                        .Include(d => d.Specialty)
                        .FirstOrDefaultAsync(d => d.DoctorId == appointmentDto.DoctorId.Value &&
                                                  d.ProviderId == appointmentDto.ProviderId);

                    if (doctor == null)
                        return ApiResponseHelper.Error("الطبيب غير موجود أو لا يعمل في هذا المركز", 400);
                }

                // Validate specialty if specified
                if (appointmentDto.SpecialtyId.HasValue)
                {
                    var specialty = await _context.MedicalSpecialties
                        .FirstOrDefaultAsync(s => s.SpecialtyId == appointmentDto.SpecialtyId.Value);

                    if (specialty == null)
                        return ApiResponseHelper.Error("التخصص غير موجود", 400);
                }

                // Check for time slot availability
                var endTime = appointmentDto.ScheduledAt.AddMinutes(appointmentDto.DurationMinutes);
                var conflictingAppointment = await _context.Appointments
                    .Where(a => a.ProviderId == appointmentDto.ProviderId &&
                                a.StatusId != GetCancelledStatusId() && // Not cancelled
                                a.ScheduledAt < endTime &&
                                a.ScheduledAt.AddMinutes(a.DurationMinutes) > appointmentDto.ScheduledAt &&
                                (appointmentDto.DoctorId == null || a.DoctorId == appointmentDto.DoctorId))
                    .FirstOrDefaultAsync();

                if (conflictingAppointment != null)
                    return ApiResponseHelper.Error("هذا الموعد متعارض مع موعد آخر", 400);

                // Generate unique appointment code
                var appointmentCode = GenerateAppointmentCode();

                // Calculate exchange rate if needed
                decimal? exchangeRate = null;
                if (appointmentDto.PriceSYP > 0 && appointmentDto.PriceUSD > 0)
                {
                    exchangeRate = appointmentDto.PriceSYP / appointmentDto.PriceUSD;
                }

                // Create appointment
                var appointment = new Appointment
                {
                    AppointmentCode = appointmentCode,
                    PatientId = appointmentDto.PatientId,
                    ProviderId = appointmentDto.ProviderId,
                    DoctorId = appointmentDto.DoctorId,
                    SpecialtyId = appointmentDto.SpecialtyId,
                    ScheduledAt = appointmentDto.ScheduledAt,
                    DurationMinutes = appointmentDto.DurationMinutes,
                    StatusId = GetPendingStatusId(),
                    PriceSyp = appointmentDto.PriceSYP,
                    PriceUsd = appointmentDto.PriceUSD,
                    ExchangeRate = exchangeRate,
                    IsPaid = false,
                    BookingSource = appointmentDto.BookingSource ?? "Mobile",
                    //Notes = appointmentDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();

                // Create notification for patient
                var patientNotification = new Notification
                {
                    UserId = patient.UserId,
                    Title = "تم حجز موعد جديد",
                 //   Message = $"تم حجز موعد لك في {provider.ProviderName} بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(patientNotification);

                // Create notification for provider
                var providerUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == provider.UserId);
                if (providerUser != null)
                {
                    var providerNotification = new Notification
                    {
                        UserId = providerUser.UserId,
                        Title = "موعد جديد",
                       // Message = $"تم حجز موعد جديد مع المريض {patient.User.FullName} بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
                        Channel = "InApp",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Notifications.AddAsync(providerNotification);
                }

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Create Appointment",
                    Entity = "Appointment",
                    EntityId = appointment.AppointmentId.ToString(),
                    Details = $"Created appointment {appointmentCode} for patient {patient.User.FullName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Appointment created: {AppointmentCode} for patient {PatientId}",
                    appointment.AppointmentCode, appointment.PatientId);

                return ApiResponseHelper.Success(new
                {
                    AppointmentId = appointment.AppointmentId,
                    AppointmentCode = appointment.AppointmentCode,
                    ScheduledAt = appointment.ScheduledAt,
                    Status = "Pending"
                }, "تم حجز الموعد بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating appointment");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء حجز الموعد");
            }
        }

        public async Task<ApiResponse> UpdateAppointmentStatusAsync(int appointmentId, int statusId, int currentUserId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Provider)
                    .Include(a => a.Status)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                    return ApiResponseHelper.NotFound("الموعد غير موجود");

                // Validate status exists
                var status = await _context.AppointmentStatuses
                    .FirstOrDefaultAsync(s => s.StatusId == statusId);

                if (status == null)
                    return ApiResponseHelper.Error("حالة غير صالحة", 400);

                // Check if status transition is valid
                if (!IsValidStatusTransition(appointment.StatusId, statusId))
                    return ApiResponseHelper.Error("تحول الحالة غير مسموح به", 400);

                var oldStatusName = appointment.Status.StatusName;
                appointment.StatusId = statusId;
                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create notification for status change
                var notification = new Notification
                {
                    UserId = appointment.Patient.UserId,
                    Title = $"تغيير حالة الموعد",
                    //Message = $"تم تغيير حالة موعدك في {appointment.Provider.ProviderName} من {oldStatusName} إلى {status.StatusName}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Appointment Status",
                    Entity = "Appointment",
                    EntityId = appointmentId.ToString(),
                    Details = $"Changed appointment status from {oldStatusName} to {status.StatusName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} status changed from {OldStatus} to {NewStatus}",
                    appointmentId, oldStatusName, status.StatusName);

                return ApiResponseHelper.Success(new
                {
                    appointment.AppointmentId,
                    appointment.AppointmentCode,
                    OldStatus = oldStatusName,
                    NewStatus = status.StatusName
                }, "تم تحديث حالة الموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status for ID {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث حالة الموعد");
            }
        }

        public async Task<ApiResponse> CancelAppointmentAsync(int appointmentId, int currentUserId, string reason = null)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Provider)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                    return ApiResponseHelper.NotFound("الموعد غير موجود");

                // Get cancelled status ID
                var cancelledStatusId = GetCancelledStatusId();

                // Check if appointment can be cancelled
                if (appointment.StatusId == cancelledStatusId)
                    return ApiResponseHelper.Error("الموعد ملغي بالفعل", 400);

                if (appointment.StatusId == GetCompletedStatusId())
                    return ApiResponseHelper.Error("لا يمكن إلغاء موعد مكتمل", 400);

                // Check cancellation window (e.g., cannot cancel less than 24 hours before appointment)
                var timeUntilAppointment = appointment.ScheduledAt - DateTime.UtcNow;
                if (timeUntilAppointment.TotalHours < 24)
                    return ApiResponseHelper.Error("لا يمكن إلغاء الموعد قبل أقل من 24 ساعة من الموعد", 400);

                appointment.StatusId = cancelledStatusId;
                appointment.CancelReason = reason ?? appointment.CancelReason;
                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create notification for cancellation
                var notification = new Notification
                {
                    UserId = appointment.Patient.UserId,
                    Title = "تم إلغاء الموعد",
                   // Message = $"تم إلغاء موعدك في {appointment.Provider.ProviderName} بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}" +
                         //    (reason != null ? $"\nالسبب: {reason}" : ""),
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Cancel Appointment",
                    Entity = "Appointment",
                    EntityId = appointmentId.ToString(),
                    Details = $"Cancelled appointment {appointment.AppointmentCode}. Reason: {reason}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} cancelled by user {UserId}",
                    appointmentId, currentUserId);

                return ApiResponseHelper.Success(new
                {
                    appointment.AppointmentId,
                    appointment.AppointmentCode,
                    Status = "Cancelled",
                    CancellationReason = reason
                }, "تم إلغاء الموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إلغاء الموعد");
            }
        }

        public async Task<ApiResponse> CompleteAppointmentAsync(int appointmentId, int currentUserId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                    return ApiResponseHelper.NotFound("الموعد غير موجود");

                // Get completed status ID
                var completedStatusId = GetCompletedStatusId();

                // Check if appointment can be marked as completed
                if (appointment.StatusId != GetConfirmedStatusId())
                    return ApiResponseHelper.Error("يمكن إكمال الموعد المؤكد فقط", 400);

                appointment.StatusId = completedStatusId;
                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create notification for completion
                var notification = new Notification
                {
                    UserId = appointment.Patient.UserId,
                    Title = "تم إكمال الموعد",
                   // Message = $"تم إكمال موعدك بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Complete Appointment",
                    Entity = "Appointment",
                    EntityId = appointmentId.ToString(),
                    Details = $"Completed appointment {appointment.AppointmentCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} marked as completed by user {UserId}",
                    appointmentId, currentUserId);

                return ApiResponseHelper.Success(new
                {
                    appointment.AppointmentId,
                    appointment.AppointmentCode,
                    Status = "Completed"
                }, "تم إكمال الموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إكمال الموعد");
            }
        }

        public async Task<ApiResponse> GetProviderAvailabilityAsync(int providerId, int? doctorId, DateTime date)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.ProviderDoctors)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                // Get existing appointments for the date
                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1).AddTicks(-1);

                var existingAppointments = await _context.Appointments
                    .Where(a => a.ProviderId == providerId &&
                                a.StatusId != GetCancelledStatusId() &&
                                a.ScheduledAt >= startOfDay &&
                                a.ScheduledAt <= endOfDay &&
                                (doctorId == null || a.DoctorId == doctorId))
                    .Select(a => new
                    {
                        a.ScheduledAt,
                        a.DurationMinutes,
                        a.DoctorId
                    })
                    .ToListAsync();

                // Define working hours (9 AM to 5 PM)
                var workingHoursStart = TimeSpan.FromHours(9);
                var workingHoursEnd = TimeSpan.FromHours(17);
                var slotDuration = 30; // minutes

                var availableSlots = new List<object>();
                var currentTime = startOfDay.Add(workingHoursStart);

                while (currentTime.TimeOfDay < workingHoursEnd)
                {
                    var slotEnd = currentTime.AddMinutes(slotDuration);

                    // Check if slot is available
                    var isAvailable = !existingAppointments.Any(a =>
                    {
                        var appointmentEnd = a.ScheduledAt.AddMinutes(a.DurationMinutes);
                        return currentTime < appointmentEnd && slotEnd > a.ScheduledAt;
                    });

                    if (isAvailable)
                    {
                        availableSlots.Add(new
                        {
                            StartTime = currentTime,
                            EndTime = slotEnd,
                            DurationMinutes = slotDuration
                        });
                    }

                    currentTime = currentTime.AddMinutes(slotDuration);
                }

                // Get available doctors for this provider
                var availableDoctors = await _context.ProviderDoctors
                    .Include(d => d.Specialty)
                    .Where(d => d.ProviderId == providerId && d.IsActive)
                    .Select(d => new
                    {
                        d.DoctorId,
                        d.FullName,
                        Specialty = d.Specialty != null ? d.Specialty.SpecialtyName : null,
                        d.WorkingHours
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    ProviderId = providerId,
                    ProviderName = provider.ProviderName,
                    Date = date.Date,
                    WorkingHours = $"{workingHoursStart:hh\\:mm} - {workingHoursEnd:hh\\:mm}",
                    SlotDuration = slotDuration,
                    AvailableSlots = availableSlots,
                    TotalAvailableSlots = availableSlots.Count,
                    AvailableDoctors = availableDoctors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting availability for provider {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الأوقات المتاحة");
            }
        }

        public async Task<ApiResponse> GetPatientAppointmentsAsync(int patientId, int page = 1, int pageSize = 20)
        {
            return await GetAppointmentsAsync(patientId, null, null, null, null, page, pageSize);
        }

        public async Task<ApiResponse> GetProviderAppointmentsAsync(int providerId, int? statusId = null,
            DateTime? date = null, int page = 1, int pageSize = 20)
        {
            DateTime? dateFrom = null;
            DateTime? dateTo = null;

            if (date.HasValue)
            {
                dateFrom = date.Value.Date;
                dateTo = date.Value.Date.AddDays(1).AddTicks(-1);
            }

            return await GetAppointmentsAsync(null, providerId, statusId, dateFrom, dateTo, page, pageSize);
        }

        public async Task<ApiResponse> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateDto, int currentUserId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                    return ApiResponseHelper.NotFound("الموعد غير موجود");

                // Check if appointment can be updated
                if (appointment.StatusId == GetCompletedStatusId() || appointment.StatusId == GetCancelledStatusId())
                    return ApiResponseHelper.Error("لا يمكن تعديل الموعد المكتمل أو الملغي", 400);

                // Update fields if provided
                if (updateDto.ScheduledAt.HasValue)
                {
                    // Check for conflicts if rescheduling
                    var endTime = updateDto.ScheduledAt.Value.AddMinutes(updateDto.DurationMinutes ?? appointment.DurationMinutes);
                    var conflictingAppointment = await _context.Appointments
                        .Where(a => a.AppointmentId != appointmentId &&
                                    a.ProviderId == appointment.ProviderId &&
                                    a.StatusId != GetCancelledStatusId() &&
                                    a.ScheduledAt < endTime &&
                                    a.ScheduledAt.AddMinutes(a.DurationMinutes) > updateDto.ScheduledAt.Value &&
                                    (appointment.DoctorId == null || a.DoctorId == appointment.DoctorId))
                        .FirstOrDefaultAsync();

                    if (conflictingAppointment != null)
                        return ApiResponseHelper.Error("هذا الموعد الجديد متعارض مع موعد آخر", 400);

                    appointment.ScheduledAt = updateDto.ScheduledAt.Value;
                }

                if (updateDto.DurationMinutes.HasValue)
                    appointment.DurationMinutes = updateDto.DurationMinutes.Value;

                if (!string.IsNullOrEmpty(updateDto.Notes))
                   // appointment.Notes = updateDto.Notes;

                if (!string.IsNullOrEmpty(updateDto.CancelReason))
                    appointment.CancelReason = updateDto.CancelReason;

                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Appointment",
                    Entity = "Appointment",
                    EntityId = appointmentId.ToString(),
                    Details = $"Updated appointment {appointment.AppointmentCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    appointment.AppointmentId,
                    appointment.AppointmentCode,
                    appointment.ScheduledAt,
                    appointment.DurationMinutes,
                    Status = GetStatusName(appointment.StatusId)
                }, "تم تحديث الموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث الموعد");
            }
        }

        public async Task<ApiResponse> AddMedFileToAppointmentAsync(int appointmentId, int medFileId, int currentUserId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                    return ApiResponseHelper.NotFound("الموعد غير موجود");

                var medFile = await _context.MedFiles
                    .FirstOrDefaultAsync(m => m.MedFileId == medFileId);

                if (medFile == null)
                    return ApiResponseHelper.NotFound("الملف الطبي غير موجود");

                // Check if file is already attached to this appointment
                var existingLink = await _context.AppointmentMedFiles
                    .FirstOrDefaultAsync(amf => amf.AppointmentId == appointmentId && amf.MedFileId == medFileId);

                if (existingLink != null)
                    return ApiResponseHelper.Error("الملف مرفق بالفعل بهذا الموعد", 400);

                var appointmentMedFile = new AppointmentMedFile
                {
                    AppointmentId = appointmentId,
                    MedFileId = medFileId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.AppointmentMedFiles.AddAsync(appointmentMedFile);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Add MedFile to Appointment",
                    Entity = "Appointment",
                    EntityId = appointmentId.ToString(),
                    Details = $"Added med file {medFile.MedFileName} to appointment {appointment.AppointmentCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    AppointmentMedFileId = appointmentMedFile.AppointmentMedFileId,
                    appointment.AppointmentId,
                    medFile.MedFileId,
                    medFile.MedFileName
                }, "تم إرفاق الملف الطبي بالموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding med file to appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إرفاق الملف الطبي");
            }
        }

        #region Helper Methods

        private string GenerateAppointmentCode()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            return $"APT-{datePart}-{randomPart}";
        }

        private int GetPendingStatusId()
        {
            return _context.AppointmentStatuses
                .FirstOrDefault(s => s.StatusName == "Pending")?.StatusId ?? 1;
        }

        private int GetConfirmedStatusId()
        {
            return _context.AppointmentStatuses
                .FirstOrDefault(s => s.StatusName == "Confirmed")?.StatusId ?? 2;
        }

        private int GetCompletedStatusId()
        {
            return _context.AppointmentStatuses
                .FirstOrDefault(s => s.StatusName == "Completed")?.StatusId ?? 3;
        }

        private int GetCancelledStatusId()
        {
            return _context.AppointmentStatuses
                .FirstOrDefault(s => s.StatusName == "Cancelled")?.StatusId ?? 4;
        }

        private string GetStatusName(int statusId)
        {
            return _context.AppointmentStatuses
                .FirstOrDefault(s => s.StatusId == statusId)?.StatusName ?? "Unknown";
        }

        private bool IsValidStatusTransition(int currentStatusId, int newStatusId)
        {
            // Define valid transitions
            var transitions = new Dictionary<int, List<int>>
            {
                { GetPendingStatusId(), new List<int> { GetConfirmedStatusId(), GetCancelledStatusId() } },
                { GetConfirmedStatusId(), new List<int> { GetCompletedStatusId(), GetCancelledStatusId() } },
                { GetCompletedStatusId(), new List<int>() }, // No transitions from completed
                { GetCancelledStatusId(), new List<int>() }  // No transitions from cancelled
            };

            return transitions.ContainsKey(currentStatusId) &&
                   transitions[currentStatusId].Contains(newStatusId);
        }

        // ----- Added: centralized activity logger -----
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
                _logger.LogError(ex, "Failed to log activity: {Action} {Entity} {EntityId}", action, entity, entityId);
            }
        }

        #endregion

        // Explicit interface implementations to ensure IAppointmentService is fully satisfied
        Task<ApiResponse> IAppointmentService.GetDoctorAvailabilitySlotsAsync(int doctorId, DateTime date)
            => GetDoctorAvailabilitySlotsAsync(doctorId, date);

        Task<ApiResponse> IAppointmentService.CreateDoctorAvailabilityAsync(int doctorId, CreateDoctorAvailabilityDto dto, int currentUserId)
            => CreateDoctorAvailabilityAsync(doctorId, dto, currentUserId);

        Task<ApiResponse> IAppointmentService.UpdateDoctorAvailabilityAsync(int doctorId, int availabilityId, UpdateDoctorAvailabilityDto dto, int currentUserId)
            => UpdateDoctorAvailabilityAsync(doctorId, availabilityId, dto, currentUserId);

        Task<ApiResponse> IAppointmentService.DeleteDoctorAvailabilityAsync(int doctorId, int availabilityId, int currentUserId)
            => DeleteDoctorAvailabilityAsync(doctorId, availabilityId, currentUserId);

        Task<ApiResponse> IAppointmentService.AcceptAppointmentAsync(int appointmentId, int currentUserId)
            => AcceptAppointmentAsync(appointmentId, currentUserId);

        Task<ApiResponse> IAppointmentService.RejectAppointmentAsync(int appointmentId, int currentUserId, string? reason)
            => RejectAppointmentAsync(appointmentId, currentUserId, reason);

        Task<ApiResponse> IAppointmentService.RescheduleAppointmentAsync(int appointmentId, DateTime newScheduledAt, int durationMinutes, int currentUserId)
            => RescheduleAppointmentAsync(appointmentId, newScheduledAt, durationMinutes, currentUserId);
        public async Task<ApiResponse> GetDoctorAvailabilitySlotsAsync(int doctorId, DateTime date)
        {
            try
            {
                var doctor = await _context.ProviderDoctors
                    .Include(d => d.DoctorAvailabilities)
                    .FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.IsActive);

                if (doctor == null)
                    return ApiResponseHelper.NotFound("الطبيب غير موجود");

                var dayOfWeek = (int)date.DayOfWeek; // 0..6

                var availabilities = doctor.DoctorAvailabilities
                    .Where(a => a.DayOfWeek == dayOfWeek && a.IsActive)
                    .ToList();

                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1).AddTicks(-1);

                var existingAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.StatusId != GetCancelledStatusId() &&
                                a.ScheduledAt >= startOfDay && a.ScheduledAt <= endOfDay)
                    .Select(a => new { a.ScheduledAt, a.DurationMinutes })
                    .ToListAsync();

                var slots = new List<DoctorAvailabilitySlotDto>();

                foreach (var av in availabilities)
                {
                    var slotDuration = av.SlotDurationMinutes > 0 ? av.SlotDurationMinutes : 30;
                    var current = startOfDay.Add(av.StartTime.ToTimeSpan());
                    var availEnd = startOfDay.Add(av.EndTime.ToTimeSpan());

                    while (current.AddMinutes(slotDuration) <= availEnd)
                    {
                        var slotStart = current;
                        var slotEnd = current.AddMinutes(slotDuration);

                        var conflict = existingAppointments.Any(a =>
                        {
                            var aStart = a.ScheduledAt;
                            var aEnd = a.ScheduledAt.AddMinutes(a.DurationMinutes);
                            return slotStart < aEnd && slotEnd > aStart;
                        });

                        slots.Add(new DoctorAvailabilitySlotDto
                        {
                            Start = slotStart,
                            End = slotEnd,
                            IsAvailable = !conflict
                        });

                        current = current.AddMinutes(slotDuration);
                    }
                }

                return ApiResponseHelper.Success(new
                {
                    DoctorId = doctorId,
                    DoctorName = doctor.FullName,
                    Date = date.Date,
                    Slots = slots.OrderBy(s => s.Start).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building availability for doctor {DoctorId}", doctorId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء جلب التوافر");
            }
        }

        public async Task<ApiResponse> CreateDoctorAvailabilityAsync(int doctorId, CreateDoctorAvailabilityDto dto, int currentUserId)
        {
            try
            {
                var doctor = await _context.ProviderDoctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId);
                if (doctor == null) return ApiResponseHelper.NotFound("الطبيب غير موجود");

                // Permission: provider owner or admin
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.ProviderId == doctor.ProviderId);
                var currentProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var isAdmin = await _context.Users.Include(u => u.Role).AnyAsync(u => u.UserId == currentUserId && u.Role.RoleName == "Admin");

                if (!isAdmin && currentProvider?.ProviderId != provider.ProviderId)
                    return ApiResponseHelper.Error("غير مصرح بإدارة جداول الطبيب", 403);

                var availability = new DoctorAvailability
                {
                    DoctorId = doctorId,
                    DayOfWeek = dto.DayOfWeek,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    SlotDurationMinutes = dto.SlotDurationMinutes,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.DoctorAvailabilities.AddAsync(availability);
                await _context.SaveChangesAsync();

                await LogActivityAsync(currentUserId, "Create Doctor Availability", "DoctorAvailability", availability.AvailabilityId.ToString(),
                    $"Created availability for doctor {doctorId}");

                return ApiResponseHelper.Success(new { availability.AvailabilityId }, "تم إنشاء جدول التوافر بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating availability for doctor {DoctorId}", doctorId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إنشاء جدول التوافر");
            }
        }

        public async Task<ApiResponse> UpdateDoctorAvailabilityAsync(int doctorId, int availabilityId, UpdateDoctorAvailabilityDto dto, int currentUserId)
        {
            try
            {
                var availability = await _context.DoctorAvailabilities.FirstOrDefaultAsync(a => a.AvailabilityId == availabilityId && a.DoctorId == doctorId);
                if (availability == null) return ApiResponseHelper.NotFound("جدول التوافر غير موجود");

                // Permission check same as create
                var doctor = await _context.ProviderDoctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId);
                var currentProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var isAdmin = await _context.Users.Include(u => u.Role).AnyAsync(u => u.UserId == currentUserId && u.Role.RoleName == "Admin");

                if (!isAdmin && currentProvider?.ProviderId != doctor.ProviderId)
                    return ApiResponseHelper.Error("غير مصرح بتعديل جدول الطبيب", 403);

                if (dto.StartTime.HasValue) availability.StartTime = dto.StartTime.Value;
                if (dto.EndTime.HasValue) availability.EndTime = dto.EndTime.Value;
                if (dto.SlotDurationMinutes.HasValue) availability.SlotDurationMinutes = dto.SlotDurationMinutes.Value;
                if (dto.IsActive.HasValue) availability.IsActive = dto.IsActive.Value;

                //availability.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogActivityAsync(currentUserId, "Update Doctor Availability", "DoctorAvailability", availabilityId.ToString(),
                    $"Updated availability {availabilityId} for doctor {doctorId}");

                return ApiResponseHelper.Success(new { availability.AvailabilityId }, "تم تحديث جدول التوافر بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating availability {AvailabilityId} for doctor {DoctorId}", availabilityId, doctorId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث جدول التوافر");
            }
        }

        public async Task<ApiResponse> DeleteDoctorAvailabilityAsync(int doctorId, int availabilityId, int currentUserId)
        {
            try
            {
                var availability = await _context.DoctorAvailabilities.FirstOrDefaultAsync(a => a.AvailabilityId == availabilityId && a.DoctorId == doctorId);
                if (availability == null) return ApiResponseHelper.NotFound("جدول التوافر غير موجود");

                var doctor = await _context.ProviderDoctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId);
                var currentProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var isAdmin = await _context.Users.Include(u => u.Role).AnyAsync(u => u.UserId == currentUserId && u.Role.RoleName == "Admin");

                if (!isAdmin && currentProvider?.ProviderId != doctor.ProviderId)
                    return ApiResponseHelper.Error("غير مصرح بحذف جدول الطبيب", 403);

                _context.DoctorAvailabilities.Remove(availability);
                await _context.SaveChangesAsync();

                await LogActivityAsync(currentUserId, "Delete Doctor Availability", "DoctorAvailability", availabilityId.ToString(),
                    $"Deleted availability {availabilityId} for doctor {doctorId}");

                return ApiResponseHelper.Success(null, "تم حذف جدول التوافر بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting availability {AvailabilityId} for doctor {DoctorId}", availabilityId, doctorId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء حذف جدول التوافر");
            }
        }

        public async Task<ApiResponse> AcceptAppointmentAsync(int appointmentId, int currentUserId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Provider)
                    .Include(a => a.Doctor)
                    .Include(a => a.Status)
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null) return ApiResponseHelper.NotFound("الموعد غير موجود");

                if (appointment.StatusId != GetPendingStatusId())
                    return ApiResponseHelper.Error("يمكن قبول المواعيد المعلقة فقط", 400);

                var currentProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var isAdmin = await _context.Users.Include(u => u.Role).AnyAsync(u => u.UserId == currentUserId && u.Role.RoleName == "Admin");
                if (!isAdmin)
                {
                    if (currentProvider == null || currentProvider.ProviderId != appointment.ProviderId)
                        return ApiResponseHelper.Error("غير مصرح بقبول هذا الموعد", 403);
                }

                if (appointment.DoctorId.HasValue)
                {
                    var doctorId = appointment.DoctorId.Value;
                    var endTime = appointment.ScheduledAt.AddMinutes(appointment.DurationMinutes);

                    var conflict = await _context.Appointments
                        .Where(a => a.AppointmentId != appointmentId &&
                                    a.DoctorId == doctorId &&
                                    a.StatusId == GetConfirmedStatusId() &&
                                    a.ScheduledAt < endTime &&
                                    a.ScheduledAt.AddMinutes(a.DurationMinutes) > appointment.ScheduledAt)
                        .AnyAsync();

                    if (conflict)
                        return ApiResponseHelper.Error("يوجد تعارض لجدول الطبيب عند قبول الموعد", 400);
                }

                appointment.StatusId = GetConfirmedStatusId();
                appointment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var notification = new Notification
                {
                    UserId = appointment.Patient.UserId,
                    Title = "تم تأكيد الموعد",
                    //Message = $"تم تأكيد موعدك بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification);

                await LogActivityAsync(currentUserId, "Accept Appointment", "Appointment", appointmentId.ToString(), "Appointment accepted by provider/doctor");

                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new { appointment.AppointmentId, Status = "Confirmed" }, "تم قبول الموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء قبول الموعد");
            }
        }

        public async Task<ApiResponse> RejectAppointmentAsync(int appointmentId, int currentUserId, string? reason = null)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Provider)
                    .Include(a => a.Status)
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null) return ApiResponseHelper.NotFound("الموعد غير موجود");

                if (appointment.StatusId != GetPendingStatusId())
                    return ApiResponseHelper.Error("يمكن رفض المواعيد المعلقة فقط", 400);

                var currentProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var isAdmin = await _context.Users.Include(u => u.Role).AnyAsync(u => u.UserId == currentUserId && u.Role.RoleName == "Admin");
                if (!isAdmin)
                {
                    if (currentProvider == null || currentProvider.ProviderId != appointment.ProviderId)
                        return ApiResponseHelper.Error("غير مصرح برفض هذا الموعد", 403);
                }

                appointment.StatusId = GetCancelledStatusId();
                appointment.CancelReason = reason ?? "Rejected by provider/doctor";
                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var notification = new Notification
                {
                    UserId = appointment.Patient.UserId,
                    Title = "تم رفض الموعد",
                    //Message = $"لقد تم رفض موعدك بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}. {reason}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification);

                await LogActivityAsync(currentUserId, "Reject Appointment", "Appointment", appointmentId.ToString(), $"Appointment rejected. Reason: {reason}");

                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new { appointment.AppointmentId, Status = "Rejected" }, "تم رفض الموعد");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء رفض الموعد");
            }
        }

        public async Task<ApiResponse> RescheduleAppointmentAsync(int appointmentId, DateTime newScheduledAt, int durationMinutes, int currentUserId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Provider)
                    .Include(a => a.Doctor)
                    .Include(a => a.Status)
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null) return ApiResponseHelper.NotFound("الموعد غير موجود");

                if (appointment.StatusId == GetCompletedStatusId() || appointment.StatusId == GetCancelledStatusId())
                    return ApiResponseHelper.Error("لا يمكن إعادة جدولة الموعد المكتمل أو الملغي", 400);

                var currentProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var isAdmin = await _context.Users.Include(u => u.Role).AnyAsync(u => u.UserId == currentUserId && u.Role.RoleName == "Admin");
                if (!isAdmin)
                {
                    if (currentProvider == null || currentProvider.ProviderId != appointment.ProviderId)
                        return ApiResponseHelper.Error("غير مصرح بإعادة جدولة هذا الموعد", 403);
                }

                var endTime = newScheduledAt.AddMinutes(durationMinutes);
                var conflictingAppointment = await _context.Appointments
                    .Where(a => a.AppointmentId != appointmentId &&
                                a.ProviderId == appointment.ProviderId &&
                                a.StatusId != GetCancelledStatusId() &&
                                a.ScheduledAt < endTime &&
                                a.ScheduledAt.AddMinutes(a.DurationMinutes) > newScheduledAt &&
                                (appointment.DoctorId == null || a.DoctorId == appointment.DoctorId))
                    .FirstOrDefaultAsync();

                if (conflictingAppointment != null)
                    return ApiResponseHelper.Error("التوقيت الجديد يتعارض مع موعد آخر", 400);

                appointment.ScheduledAt = newScheduledAt;
                appointment.DurationMinutes = durationMinutes;
                appointment.UpdatedAt = DateTime.UtcNow;

                if (appointment.StatusId == GetPendingStatusId())
                    appointment.StatusId = GetPendingStatusId();
                else
                    appointment.StatusId = GetConfirmedStatusId();

                await _context.SaveChangesAsync();

                var notification = new Notification
                {
                    UserId = appointment.Patient.UserId,
                    Title = "تم إعادة جدولة الموعد",
                   // Message = $"تمت إعادة جدولة موعدك إلى {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification);

                await LogActivityAsync(currentUserId, "Reschedule Appointment", "Appointment", appointmentId.ToString(), $"Rescheduled to {appointment.ScheduledAt:O}");

                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    appointment.AppointmentId,
                    appointment.ScheduledAt,
                    appointment.DurationMinutes,
                    Status = GetStatusName(appointment.StatusId)
                }, "تمت إعادة جدولة الموعد بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إعادة جدولة الموعد");
            }
        }
    }
}