using Microsoft.EntityFrameworkCore;
using LifeLink_V2.Data;
using LifeLink_V2.Models;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeLink_V2.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AppointmentService> _logger;

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
                    appointment.Notes,
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
                    Notes = appointmentDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();

                // Create notification for patient
                var patientNotification = new Notification
                {
                    UserId = patient.UserId,
                    Title = "تم حجز موعد جديد",
                    Message = $"تم حجز موعد لك في {provider.ProviderName} بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
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
                        Message = $"تم حجز موعد جديد مع المريض {patient.User.FullName} بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
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
                    Message = $"تم تغيير حالة موعدك في {appointment.Provider.ProviderName} من {oldStatusName} إلى {status.StatusName}",
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
                    Message = $"تم إلغاء موعدك في {appointment.Provider.ProviderName} بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}" +
                             (reason != null ? $"\nالسبب: {reason}" : ""),
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
                    Message = $"تم إكمال موعدك بتاريخ {appointment.ScheduledAt:yyyy-MM-dd HH:mm}",
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
                    appointment.Notes = updateDto.Notes;

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

        #endregion
    }
}