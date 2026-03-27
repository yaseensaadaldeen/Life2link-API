using LifeLink_V2.DTOs.Patient;
using LifeLink_V2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeLink_V2.Helpers;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(IPatientService patientService, ILogger<PatientController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        // POST: api/patient (Admin/Provider only)
        [HttpPost]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> CreatePatient([FromBody] CreatePatientDto createDto)
        {
            _logger.LogInformation("Creating new patient by user: {UserId}", GetCurrentUserId());

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = errors
                });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "غير مصرح"
                });
            }

            var result = await _patientService.CreatePatientAsync(createDto, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetPatient), new { id = ((dynamic)result.Data).PatientId }, result);
        }

        // GET: api/patient/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetPatient(int id)
        {
            // Patients can view their own profile, admins/providers can view any
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            // Check if user is trying to access their own patient profile
            var isOwnProfile = false;
            if (currentUserRole == "Patient")
            {
                var patient = await _patientService.GetPatientByUserIdAsync(currentUserId.Value);
                if (patient.Success)
                {
                    var patientData = (dynamic)patient.Data;
                    if (patientData.PatientId == id)
                        isOwnProfile = true;
                }
            }

            // Allow access if: Admin, Provider, or viewing own profile
            if (currentUserRole != "Admin" && currentUserRole != "Provider" && !isOwnProfile)
                return Forbid();

            var result = await _patientService.GetPatientByIdAsync(id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/patient/my-profile (Patient only)
        [HttpGet("my-profile")]
        [Authorize(Roles = "Patient")]
        public async Task<ActionResult<ApiResponse>> GetMyPatientProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            var result = await _patientService.GetPatientByUserIdAsync(currentUserId.Value);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // PUT: api/patient/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdatePatient(int id, [FromBody] UpdatePatientDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات",
                    Errors = errors
                });
            }

            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            // Check permissions
            var isOwnProfile = false;
            if (currentUserRole == "Patient")
            {
                var patient = await _patientService.GetPatientByUserIdAsync(currentUserId.Value);
                if (patient.Success)
                {
                    var patientData = (dynamic)patient.Data;
                    if (patientData.PatientId == id)
                        isOwnProfile = true;
                }
            }

            // Allow update if: Admin, Provider, or updating own profile
            if (currentUserRole != "Admin" && currentUserRole != "Provider" && !isOwnProfile)
                return Forbid();

            var result = await _patientService.UpdatePatientAsync(id, updateDto, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/patient/{id} (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> DeletePatient(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            var result = await _patientService.DeletePatientAsync(id, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/patient/search
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> SearchPatients(
            [FromQuery] PatientSearchDto searchDto,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _patientService.SearchPatientsAsync(searchDto, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/patient/{id}/stats
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<ApiResponse>> GetPatientStats(int id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            // Check permissions
            var isOwnProfile = false;
            if (currentUserRole == "Patient")
            {
                var patient = await _patientService.GetPatientByUserIdAsync(currentUserId.Value);
                if (patient.Success)
                {
                    var patientData = (dynamic)patient.Data;
                    if (patientData.PatientId == id)
                        isOwnProfile = true;
                }
            }

            // Allow access if: Admin, Provider, or viewing own stats
            if (currentUserRole != "Admin" && currentUserRole != "Provider" && !isOwnProfile)
                return Forbid();

            var result = await _patientService.GetPatientStatsAsync(id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/patient/recent
        [HttpGet("recent")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> GetRecentPatients([FromQuery] int count = 10)
        {
            if (count < 1 || count > 50) count = 10;

            var result = await _patientService.GetRecentPatientsAsync(count);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/patient/list (simple paginated list)
        [HttpGet("list")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> ListPatients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var searchDto = new PatientSearchDto();
            var result = await _patientService.SearchPatientsAsync(searchDto, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value;
        }
    }

   
}