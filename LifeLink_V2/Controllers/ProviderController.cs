using LifeLink_V2.DTOs.Provider;
using LifeLink_V2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeLink_V2.Helpers;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProviderController : ControllerBase
    {
        private readonly IProviderService _providerService;
        private readonly ILogger<ProviderController> _logger;

        public ProviderController(IProviderService providerService, ILogger<ProviderController> logger)
        {
            _providerService = providerService;
            _logger = logger;
        }

        // =========== PROVIDER CRUD ===========

        // POST: api/provider (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> CreateProvider([FromBody] CreateProviderDto createDto)
        {
            _logger.LogInformation("Creating new provider by admin: {UserId}", GetCurrentUserId());

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

            var result = await _providerService.CreateProviderAsync(createDto, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetProvider), new { id = ((dynamic)result.Data).ProviderId }, result);
        }

        // GET: api/provider/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetProvider(int id)
        {
            // Providers can view their own profile, admins can view any, patients can view for booking
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "Provider")
            {
                var currentUserId = GetCurrentUserId();
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);

                if (provider.Success)
                {
                    var providerData = (dynamic)provider.Data;
                    if (providerData.ProviderId != id)
                        return Forbid(); // Provider can only view their own
                }
            }

            var result = await _providerService.GetProviderByIdAsync(id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/provider/my-profile (Provider only)
        [HttpGet("my-profile")]
        [Authorize(Roles = "Provider")]
        public async Task<ActionResult<ApiResponse>> GetMyProviderProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            var result = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // PUT: api/provider/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateProvider(int id, [FromBody] UpdateProviderDto updateDto)
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
            if (currentUserRole == "Provider")
            {
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
                if (provider.Success)
                {
                    var providerData = (dynamic)provider.Data;
                    if (providerData.ProviderId != id)
                        return Forbid(); // Provider can only update their own
                }
                else
                {
                    return Forbid();
                }
            }
            else if (currentUserRole != "Admin")
            {
                return Forbid();
            }

            var result = await _providerService.UpdateProviderAsync(id, updateDto, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/provider/{id} (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> DeleteProvider(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            var result = await _providerService.DeleteProviderAsync(id, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/provider/search
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> SearchProviders(
            [FromQuery] ProviderSearchDto searchDto,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Only show active providers to non-admin users
            var currentUserRole = GetCurrentUserRole();
            if (currentUserRole != "Admin")
            {
                searchDto.IsActive = true;
            }

            var result = await _providerService.SearchProvidersAsync(searchDto, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/provider/{id}/stats
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<ApiResponse>> GetProviderStats(int id)
        {
            var currentUserRole = GetCurrentUserRole();
            var currentUserId = GetCurrentUserId();

            if (currentUserRole == "Provider")
            {
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
                if (provider.Success)
                {
                    var providerData = (dynamic)provider.Data;
                    if (providerData.ProviderId != id)
                        return Forbid(); // Provider can only view their own stats
                }
                else
                {
                    return Forbid();
                }
            }
            else if (currentUserRole != "Admin")
            {
                return Forbid();
            }

            var result = await _providerService.GetProviderStatsAsync(id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/provider/recent
        [HttpGet("recent")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> GetRecentProviders([FromQuery] int count = 10)
        {
            if (count < 1 || count > 50) count = 10;

            var result = await _providerService.GetRecentProvidersAsync(count);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/provider/list (simple paginated list)
        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse>> ListProviders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var searchDto = new ProviderSearchDto();

            // Only show active providers to non-admin users
            var currentUserRole = GetCurrentUserRole();
            if (currentUserRole != "Admin")
            {
                searchDto.IsActive = true;
            }

            var result = await _providerService.SearchProvidersAsync(searchDto, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // PUT: api/provider/{id}/status (Admin only)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateProviderStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات"
                });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            var result = await _providerService.UpdateProviderStatusAsync(id, statusDto.IsActive, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // =========== DOCTOR MANAGEMENT ===========

        // POST: api/provider/{providerId}/doctors
        [HttpPost("{providerId}/doctors")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> AddDoctor(int providerId, [FromBody] CreateDoctorDto createDto)
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
            if (currentUserRole == "Provider")
            {
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
                if (provider.Success)
                {
                    var providerData = (dynamic)provider.Data;
                    if (providerData.ProviderId != providerId)
                        return Forbid(); // Provider can only add doctors to their own
                }
                else
                {
                    return Forbid();
                }
            }

            var result = await _providerService.AddDoctorAsync(providerId, createDto, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetDoctor), new { doctorId = ((dynamic)result.Data).DoctorId }, result);
        }

        // GET: api/provider/doctors/{doctorId}
        [HttpGet("doctors/{doctorId}")]
        public async Task<ActionResult<ApiResponse>> GetDoctor(int doctorId)
        {
            var result = await _providerService.GetDoctorByIdAsync(doctorId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/provider/{providerId}/doctors
        [HttpGet("{providerId}/doctors")]
        public async Task<ActionResult<ApiResponse>> GetDoctorsByProvider(
            int providerId,
            [FromQuery] bool? activeOnly = true)
        {
            var result = await _providerService.GetDoctorsByProviderAsync(providerId, activeOnly);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/provider/my-doctors (Provider only)
        [HttpGet("my-doctors")]
        [Authorize(Roles = "Provider")]
        public async Task<ActionResult<ApiResponse>> GetMyDoctors([FromQuery] bool? activeOnly = true)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
            if (!provider.Success)
                return NotFound(provider);

            var providerData = (dynamic)provider.Data;
            var result = await _providerService.GetDoctorsByProviderAsync(providerData.ProviderId, activeOnly);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // PUT: api/provider/doctors/{doctorId}
        [HttpPut("doctors/{doctorId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> UpdateDoctor(int doctorId, [FromBody] UpdateDoctorDto updateDto)
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
            if (currentUserRole == "Provider")
            {
                // Get the doctor to check if they belong to this provider
                var doctor = await _providerService.GetDoctorByIdAsync(doctorId);
                if (!doctor.Success)
                    return NotFound(doctor);

                var doctorData = (dynamic)doctor.Data;

                // Get provider of current user
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
                if (!provider.Success)
                    return Forbid();

                var providerData = (dynamic)provider.Data;

                if (doctorData.ProviderId != providerData.ProviderId)
                    return Forbid(); // Provider can only update their own doctors
            }

            var result = await _providerService.UpdateDoctorAsync(doctorId, updateDto, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/provider/doctors/{doctorId}
        [HttpDelete("doctors/{doctorId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> DeleteDoctor(int doctorId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            // Check permissions
            if (currentUserRole == "Provider")
            {
                // Get the doctor to check if they belong to this provider
                var doctor = await _providerService.GetDoctorByIdAsync(doctorId);
                if (!doctor.Success)
                    return NotFound(doctor);

                var doctorData = (dynamic)doctor.Data;

                // Get provider of current user
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
                if (!provider.Success)
                    return Forbid();

                var providerData = (dynamic)provider.Data;

                if (doctorData.ProviderId != providerData.ProviderId)
                    return Forbid(); // Provider can only delete their own doctors
            }

            var result = await _providerService.DeleteDoctorAsync(doctorId, currentUserId.Value);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/provider/doctors/{doctorId}/stats
        [HttpGet("doctors/{doctorId}/stats")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> GetDoctorStats(int doctorId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "غير مصرح" });

            // Check permissions
            if (currentUserRole == "Provider")
            {
                // Get the doctor to check if they belong to this provider
                var doctor = await _providerService.GetDoctorByIdAsync(doctorId);
                if (!doctor.Success)
                    return NotFound(doctor);

                var doctorData = (dynamic)doctor.Data;

                // Get provider of current user
                var provider = await _providerService.GetProviderByUserIdAsync(currentUserId.Value);
                if (!provider.Success)
                    return Forbid();

                var providerData = (dynamic)provider.Data;

                if (doctorData.ProviderId != providerData.ProviderId)
                    return Forbid(); // Provider can only view stats of their own doctors
            }

            var result = await _providerService.GetDoctorStatsAsync(doctorId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/provider/types
        [HttpGet("types")]
        public async Task<ActionResult<ApiResponse>> GetProviderTypes()
        {
            // This would typically come from StaticDataController, but added here for convenience
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "أنواع المؤسسات الطبية",
                Data = new[]
                {
                    new { Id = 1, Name = "Clinic", Description = "عيادة طبية" },
                    new { Id = 2, Name = "Hospital", Description = "مستشفى" },
                    new { Id = 3, Name = "Pharmacy", Description = "صيدلية" },
                    new { Id = 4, Name = "Laboratory", Description = "مختبر" },
                    new { Id = 5, Name = "Diagnostic Center", Description = "مركز تشخيصي" }
                }
            });
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