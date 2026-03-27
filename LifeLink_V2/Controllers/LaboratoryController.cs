using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LaboratoryController : ControllerBase
    {
        private readonly ILaboratoryService _laboratoryService;
        private readonly ILogger<LaboratoryController> _logger;

        public LaboratoryController(ILaboratoryService laboratoryService, ILogger<LaboratoryController> logger)
        {
            _laboratoryService = laboratoryService;
            _logger = logger;
        }

        #region Lab Tests Endpoints

        // GET: api/laboratory/{providerId}/tests
        [HttpGet("{providerId}/tests")]
        public async Task<IActionResult> GetLabTests(int providerId,
            [FromQuery] int? categoryId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _laboratoryService.GetLabTestsAsync(providerId, categoryId, search, page, pageSize);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabTests for provider {ProviderId}", providerId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/laboratory/tests/{id}
        [HttpGet("tests/{id}")]
        public async Task<IActionResult> GetLabTest(int id)
        {
            try
            {
                var result = await _laboratoryService.GetLabTestByIdAsync(id);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabTest for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/tests
        [HttpPost("tests")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> AddLabTest([FromBody] AddLabTestDto request)
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

                var result = await _laboratoryService.AddLabTestAsync(request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddLabTest");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // PUT: api/laboratory/tests/{id}
        [HttpPut("tests/{id}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdateLabTest(int id, [FromBody] UpdateLabTestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.UpdateLabTestAsync(id, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateLabTest for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // DELETE: api/laboratory/tests/{id}
        [HttpDelete("tests/{id}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> DeleteLabTest(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.DeleteLabTestAsync(id, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteLabTest for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Lab Test Categories Endpoints

        // GET: api/laboratory/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetLabTestCategories()
        {
            try
            {
                var result = await _laboratoryService.GetLabTestCategoriesAsync();
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabTestCategories");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/categories
        [HttpPost("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddLabTestCategory([FromBody] AddLabTestCategoryDto request)
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

                var result = await _laboratoryService.AddLabTestCategoryAsync(request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddLabTestCategory");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Lab Orders Endpoints

        // GET: api/laboratory/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetLabOrders(
            [FromQuery] int? patientId,
            [FromQuery] int? providerId,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
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

                var result = await _laboratoryService.GetLabOrdersAsync(
                    patientId, providerId, status, parsedDateFrom, parsedDateTo, page, pageSize);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabOrders");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/laboratory/orders/{id}
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetLabOrder(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.GetLabOrderByIdAsync(id);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabOrder for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/orders
        [HttpPost("orders")]
        public async Task<IActionResult> CreateLabOrder([FromBody] CreateLabOrderDto request)
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

                var result = await _laboratoryService.CreateLabOrderAsync(request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateLabOrder");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/orders/{id}/status
        [HttpPost("orders/{id}/status")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdateLabOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                if (string.IsNullOrEmpty(request.Status))
                    return BadRequest(ApiResponseHelper.Error("يجب تحديد الحالة"));

                var result = await _laboratoryService.UpdateLabOrderStatusAsync(id, request.Status, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateLabOrderStatus for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/orders/{id}/cancel
        [HttpPost("orders/{id}/cancel")]
        public async Task<IActionResult> CancelLabOrder(int id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.CancelLabOrderAsync(id, currentUserId.Value, request?.Reason);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelLabOrder for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/orders/{id}/complete
        [HttpPost("orders/{id}/complete")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> CompleteLabOrder(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.CompleteLabOrderAsync(id, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteLabOrder for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Lab Results Endpoints

        // GET: api/laboratory/orders/{orderId}/result
        [HttpGet("orders/{orderId}/result")]
        public async Task<IActionResult> GetLabResult(int orderId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.GetLabResultByOrderIdAsync(orderId);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLabResult for order {OrderId}", orderId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/laboratory/orders/{orderId}/result
        [HttpPost("orders/{orderId}/result")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> SubmitLabResult(int orderId, [FromBody] SubmitLabResultDto request)
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

                var result = await _laboratoryService.SubmitLabResultAsync(orderId, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitLabResult for order {OrderId}", orderId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // PUT: api/laboratory/results/{id}
        [HttpPut("results/{id}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdateLabResult(int id, [FromBody] UpdateLabResultDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.UpdateLabResultAsync(id, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateLabResult for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Patient History Endpoints

        // GET: api/laboratory/patient/{patientId}/history
        [HttpGet("patient/{patientId}/history")]
        public async Task<IActionResult> GetPatientLabHistory(int patientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _laboratoryService.GetPatientLabHistoryAsync(patientId, page, pageSize);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPatientLabHistory for patient {PatientId}", patientId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Dashboard Endpoints

        // GET: api/laboratory/{providerId}/dashboard
        [HttpGet("{providerId}/dashboard")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> GetLaboratoryDashboard(int providerId,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _laboratoryService.GetLaboratoryDashboardAsync(providerId, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLaboratoryDashboard for provider {ProviderId}", providerId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        private int GetStatusCode(ApiResponse response)
        {
            if (response.Success)
                return 200;

            return 400;
        }

        #endregion
    }
}