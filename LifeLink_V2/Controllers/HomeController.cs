using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeLink_V2.Services.Interfaces;
using LifeLink_V2.Helpers;
using LifeLink_V2.DTOs.Provider;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(IAppointmentService appointmentService, ILogger<DoctorsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        // GET: /api/doctors/{id}/availability?date=2026-01-01
        [HttpGet("{id}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailability(int id, [FromQuery] string? date)
        {
            try
            {
                DateTime target = DateTime.Today;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var d)) target = d;

                var result = await _appointmentService.GetDoctorAvailabilitySlotsAsync(id, target.Date);
                return StatusCode(result.Success ? 200 : 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailability for doctor {DoctorId}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: /api/doctors/{id}/availability
        [HttpPost("{id}/availability")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> CreateAvailability(int id, [FromBody] CreateDoctorAvailabilityDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseHelper.ValidationError(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointment_service_safe_call(id, dto, currentUserId.Value);
                return StatusCode(result.Success ? 201 : 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating availability for doctor {DoctorId}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // PUT: /api/doctors/{id}/availability/{availabilityId}
        [HttpPut("{id}/availability/{availabilityId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdateAvailability(int id, int availabilityId, [FromBody] UpdateDoctorAvailabilityDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.UpdateDoctorAvailabilityAsync(id, availabilityId, dto, currentUserId.Value);
                return StatusCode(result.Success ? 200 : 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating availability {AvailabilityId} for doctor {DoctorId}", availabilityId, id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // DELETE: /api/doctors/{id}/availability/{availabilityId}
        [HttpDelete("{id}/availability/{availabilityId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> DeleteAvailability(int id, int availabilityId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _appointmentService.DeleteDoctorAvailabilityAsync(id, availabilityId, currentUserId.Value);
                return StatusCode(result.Success ? 200 : 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting availability {AvailabilityId} for doctor {DoctorId}", availabilityId, id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // small wrapper to call service while keeping controller tidy
        private async Task<ApiResponse> _appointment_service_safe_call(int doctorId, CreateDoctorAvailabilityDto dto, int currentUserId)
            => await _appointmentService.CreateDoctorAvailabilityAsync(doctorId, dto, currentUserId);

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }
    }
}