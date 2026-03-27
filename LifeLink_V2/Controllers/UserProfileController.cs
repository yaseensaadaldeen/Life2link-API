using LifeLink_V2.DTOs.User;
using LifeLink_V2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeLink_V2.Helpers;


namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(IUserService userService, ILogger<UserProfileController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: api/userprofile/my-profile
        [HttpGet("my-profile")]
        public async Task<ActionResult<ApiResponse>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "غير مصرح"
                });
            }

            var result = await _userService.GetUserProfileAsync(userId.Value);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // PUT: api/userprofile/my-profile
        [HttpPut("my-profile")]
        public async Task<ActionResult<ApiResponse>> UpdateMyProfile([FromBody] UpdateProfileDto updateDto)
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

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "غير مصرح"
                });
            }

            var result = await _userService.UpdateUserProfileAsync(userId.Value, updateDto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/userprofile/{id} (Admin only)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/userprofile/by-role/{roleName}
        [HttpGet("by-role/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> GetUsersByRole(
            string roleName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _userService.GetUsersByRoleAsync(roleName, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // PUT: api/userprofile/{id}/status (Admin only)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateUserStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "فشل التحقق من البيانات"
                });
            }

            var result = await _userService.UpdateUserStatusAsync(id, statusDto.IsActive);

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
    }

    public class UpdateStatusDto
    {
        public bool IsActive { get; set; }
    }

  
}