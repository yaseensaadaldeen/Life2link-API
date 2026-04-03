using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        // GET: api/appointments
        [HttpGet]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] int? patientId,
            [FromQuery] int? providerId,
            [FromQuery] int? statusId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Get current user ID from claims
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                // Parse dates
                DateTime? parsedDateFrom = null;
                DateTime? parsedDateTo = null;

                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df))
                    parsedDateFrom = df;

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt))
                    parsedDateTo = dt;

                var result = await _appointmentService.GetAppointmentsAsync(
                    patientId, providerId, statusId, parsedDateFrom, parsedDateTo, page, pageSize);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAppointments");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/appointments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.GetAppointmentByIdAsync(id);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponseHelper.ValidationError(errors));
                }

                var result = await _appointmentService.CreateAppointmentAsync(request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAppointment");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // PUT: api/appointments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.UpdateAppointmentAsync(id, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/confirm
        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var status = await GetStatusByName("Confirmed");
                var result = await _appointmentService.UpdateAppointmentStatusAsync(id, status, currentUserId.Value);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/complete
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.CompleteAppointmentAsync(id, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id, [FromBody] CancelAppointmentRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.CancelAppointmentAsync(id, currentUserId.Value, request?.Reason);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/appointments/availability/{providerId}
        [HttpGet("availability/{providerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailability(int providerId,
            [FromQuery] int? doctorId,
            [FromQuery] string? date)
        {
            try
            {
                DateTime parsedDate = DateTime.Today;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var d))
                    parsedDate = d;

                var result = await _appointmentService.GetProviderAvailabilityAsync(providerId, doctorId, parsedDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailability for provider {ProviderId}", providerId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/appointments/patient/{patientId}
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetPatientAppointments(int patientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                // In a real application, you would check if the current user is authorized to view these appointments
                // For now, we'll let the service handle it based on user role

                var result = await _appointmentService.GetPatientAppointmentsAsync(patientId, page, pageSize);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPatientAppointments for patient {PatientId}", patientId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/appointments/provider/{providerId}
        [HttpGet("provider/{providerId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> GetProviderAppointments(int providerId,
            [FromQuery] int? statusId,
            [FromQuery] string? date,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                DateTime? parsedDate = null;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var d))
                    parsedDate = d;

                var result = await _appointmentService.GetProviderAppointmentsAsync(
                    providerId, statusId, parsedDate, page, pageSize);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProviderAppointments for provider {ProviderId}", providerId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/medfiles/{medFileId}
        [HttpPost("{id}/medfiles/{medFileId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> AddMedFileToAppointment(int id, int medFileId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.AddMedFileToAppointmentAsync(id, medFileId, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding med file {MedFileId} to appointment {AppointmentId}", medFileId, id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        private async Task<int> GetStatusByName(string statusName)
        {
            // In a real application, you would get this from a service or database
            // For now, return based on seeded data
            return statusName switch
            {
                "Pending" => 1,
                "Confirmed" => 2,
                "Completed" => 3,
                "Cancelled" => 4,
                "NoShow" => 5,
                _ => 1
            };
        }

        private int GetStatusCode(ApiResponse response)
        {
            // Map ApiResponse success/error to HTTP status codes
            if (response.Success)
                return 200; // OK

            // You could add more sophisticated mapping based on message or error type
            return 400; // Bad Request (default for errors)
        }
        [HttpPost("request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestAppointment([FromBody] CreateAppointmentDto request)
        {
            try
            {
                // Allow unauthenticated patients to request; but require valid data
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponseHelper.ValidationError(errors));
                }

                var currentUserId = GetCurrentUserId() ?? 0; // 0 means system/anonymous — service handles validation
                var result = await _appointmentService.CreateAppointmentAsync(request, currentUserId);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RequestAppointment");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/accept
        [HttpPost("{id}/accept")]
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> AcceptAppointment(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.AcceptAppointmentAsync(id, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AcceptAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/reject
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> RejectAppointment(int id, [FromBody] CancelAppointmentRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointment_service_reject(id, currentUserId.Value, request?.Reason);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/appointments/{id}/reschedule
        [HttpPost("{id}/reschedule")]
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentRequest request)
        {
            try
            {
                if (request == null || request.NewScheduledAt == default)
                    return BadRequest(ApiResponseHelper.Error("NewScheduledAt and DurationMinutes are required", 400));

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.RescheduleAppointmentAsync(id, request.NewScheduledAt, request.DurationMinutes, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RescheduleAppointment for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        private async Task<ApiResponse> _appointment_service_reject(int appointmentId, int userId, string? reason)
            => await _appointmentService.RejectAppointmentAsync(appointmentId, userId, reason);
    }

   
    #endregion
}
public class RescheduleAppointmentRequest
{
    public DateTime NewScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
}
// Request Models for Controller
public class CancelAppointmentRequest
    {
        public string? Reason { get; set; }
    }

