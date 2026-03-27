using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PharmacyController : ControllerBase
    {
        private readonly IPharmacyService _pharmacyService;
        private readonly ILogger<PharmacyController> _logger;

        public PharmacyController(IPharmacyService pharmacyService, ILogger<PharmacyController> logger)
        {
            _pharmacyService = pharmacyService;
            _logger = logger;
        }

        #region Medicine Endpoints

        // GET: api/pharmacy/{providerId}/medicines
        [HttpGet("{providerId}/medicines")]
        public async Task<IActionResult> GetMedicines(int providerId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _pharmacyService.GetMedicinesAsync(providerId, search, page, pageSize);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMedicines for provider {ProviderId}", providerId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/pharmacy/medicines/{id}
        [HttpGet("medicines/{id}")]
        public async Task<IActionResult> GetMedicine(int id)
        {
            try
            {
                var result = await _pharmacyService.GetMedicineByIdAsync(id);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMedicine for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/pharmacy/medicines
        [HttpPost("medicines")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> AddMedicine([FromBody] AddMedicineDto request)
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

                var result = await _pharmacyService.AddMedicineAsync(request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddMedicine");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // PUT: api/pharmacy/medicines/{id}
        [HttpPut("medicines/{id}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdateMedicine(int id, [FromBody] UpdateMedicineDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.UpdateMedicineAsync(id, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateMedicine for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/pharmacy/medicines/{id}/stock
        [HttpPost("medicines/{id}/stock")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdateMedicineStock(int id, [FromBody] UpdateStockRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                if (request.QuantityChange == 0)
                    return BadRequest(ApiResponseHelper.Error("يجب تحديد كمية التغيير"));

                var result = await _pharmacyService.UpdateMedicineStockAsync(
                    id, request.QuantityChange, request.Reason ?? "تحديث مخزون", currentUserId.Value);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateMedicineStock for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // DELETE: api/pharmacy/medicines/{id}
        [HttpDelete("medicines/{id}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.DeleteMedicineAsync(id, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteMedicine for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/pharmacy/{providerId}/low-stock
        [HttpGet("{providerId}/low-stock")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> GetLowStockMedicines(int providerId, [FromQuery] int threshold = 10)
        {
            try
            {
                var result = await _pharmacyService.GetLowStockMedicinesAsync(providerId, threshold);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLowStockMedicines for provider {ProviderId}", providerId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/pharmacy/medicines/{id}/stock-history
        [HttpGet("medicines/{id}/stock-history")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> GetMedicineStockHistory(int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _pharmacyService.GetMedicineStockHistoryAsync(id, page, pageSize);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMedicineStockHistory for medicine {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Pharmacy Orders Endpoints

        // GET: api/pharmacy/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetPharmacyOrders(
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

                var result = await _pharmacyService.GetPharmacyOrdersAsync(
                    patientId, providerId, status, parsedDateFrom, parsedDateTo, page, pageSize);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPharmacyOrders");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/pharmacy/orders/{id}
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetPharmacyOrder(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.GetPharmacyOrderByIdAsync(id);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPharmacyOrder for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/pharmacy/orders
        [HttpPost("orders")]
        public async Task<IActionResult> CreatePharmacyOrder([FromBody] CreatePharmacyOrderDto request)
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

                var result = await _pharmacyService.CreatePharmacyOrderAsync(request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePharmacyOrder");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/pharmacy/orders/{id}/status
        [HttpPost("orders/{id}/status")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdatePharmacyOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                if (string.IsNullOrEmpty(request.Status))
                    return BadRequest(ApiResponseHelper.Error("يجب تحديد الحالة"));

                var result = await _pharmacyService.UpdatePharmacyOrderStatusAsync(id, request.Status, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePharmacyOrderStatus for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/pharmacy/orders/{id}/cancel
        [HttpPost("orders/{id}/cancel")]
        public async Task<IActionResult> CancelPharmacyOrder(int id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.CancelPharmacyOrderAsync(id, currentUserId.Value, request?.Reason);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelPharmacyOrder for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // POST: api/pharmacy/orders/{id}/complete
        [HttpPost("orders/{id}/complete")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> CompletePharmacyOrder(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.CompletePharmacyOrderAsync(id, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompletePharmacyOrder for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Order Items Endpoints

        // POST: api/pharmacy/orders/{orderId}/items
        [HttpPost("orders/{orderId}/items")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> AddItemToPharmacyOrder(int orderId, [FromBody] AddPharmacyOrderItemDto request)
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

                var result = await _pharmacyService.AddItemToPharmacyOrderAsync(orderId, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddItemToPharmacyOrder for order {OrderId}", orderId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // PUT: api/pharmacy/orders/{orderId}/items/{itemId}
        [HttpPut("orders/{orderId}/items/{itemId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> UpdatePharmacyOrderItem(int orderId, int itemId, [FromBody] UpdatePharmacyOrderItemDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.UpdatePharmacyOrderItemAsync(orderId, itemId, request, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePharmacyOrderItem for item {ItemId} in order {OrderId}", itemId, orderId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // DELETE: api/pharmacy/orders/{orderId}/items/{itemId}
        [HttpDelete("orders/{orderId}/items/{itemId}")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> RemoveItemFromPharmacyOrder(int orderId, int itemId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var result = await _pharmacyService.RemoveItemFromPharmacyOrderAsync(orderId, itemId, currentUserId.Value);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveItemFromPharmacyOrder for item {ItemId} in order {OrderId}", itemId, orderId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Dashboard Endpoints

        // GET: api/pharmacy/{providerId}/dashboard
        [HttpGet("{providerId}/dashboard")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<IActionResult> GetPharmacyDashboard(int providerId,
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

                var result = await _pharmacyService.GetPharmacyDashboardAsync(providerId, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPharmacyDashboard for provider {ProviderId}", providerId);
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

    // Additional Request Models for Controller
    public class UpdateStockRequest
    {
        public int QuantityChange { get; set; }
        public string? Reason { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }
}