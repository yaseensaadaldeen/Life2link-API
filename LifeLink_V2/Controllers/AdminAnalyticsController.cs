using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;

namespace LifeLink_V2.Controllers
{
    [Route("api/admin/analytics")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAnalyticsController : ControllerBase
    {
        private readonly IAdminAnalyticsService _adminAnalyticsService;
        private readonly ILogger<AdminAnalyticsController> _logger;

        public AdminAnalyticsController(IAdminAnalyticsService adminAnalyticsService, ILogger<AdminAnalyticsController> logger)
        {
            _adminAnalyticsService = adminAnalyticsService;
            _logger = logger;
        }

        #region Platform Overview

        // GET: api/admin/analytics/overview
        [HttpGet("overview")]
        public async Task<IActionResult> GetPlatformOverview(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetPlatformOverviewAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPlatformOverview");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Users Analytics

        // GET: api/admin/analytics/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsersAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetUsersAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/users/growth
        [HttpGet("users/growth")]
        public async Task<IActionResult> GetUsersGrowth([FromQuery] string period = "monthly")
        {
            try
            {
                var result = await _adminAnalyticsService.GetUsersGrowthAsync(period);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersGrowth");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Appointments Analytics

        // GET: api/admin/analytics/appointments
        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointmentsAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetAppointmentsAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAppointmentsAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/appointments/specialty
        [HttpGet("appointments/specialty")]
        public async Task<IActionResult> GetAppointmentsBySpecialty(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetAppointmentsBySpecialtyAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAppointmentsBySpecialty");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/appointments/provider-type
        [HttpGet("appointments/provider-type")]
        public async Task<IActionResult> GetAppointmentsByProviderType(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetAppointmentsByProviderTypeAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAppointmentsByProviderType");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Financial Analytics

        // GET: api/admin/analytics/revenue
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetRevenueAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRevenueAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/revenue/source
        [HttpGet("revenue/source")]
        public async Task<IActionResult> GetRevenueBySource(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetRevenueBySourceAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRevenueBySource");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/payment-methods
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethodsAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetPaymentMethodsAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentMethodsAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Providers Analytics

        // GET: api/admin/analytics/providers
        [HttpGet("providers")]
        public async Task<IActionResult> GetProvidersAnalytics()
        {
            try
            {
                var result = await _adminAnalyticsService.GetProvidersAnalyticsAsync();
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProvidersAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/providers/top
        [HttpGet("providers/top")]
        public async Task<IActionResult> GetTopProviders([FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int top = 10
           )
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetTopProvidersAsync(top, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTopProviders");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/providers/{id}/performance
        [HttpGet("providers/{id}/performance")]
        public async Task<IActionResult> GetProviderPerformance(int id,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetProviderPerformanceAsync(id, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProviderPerformance for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Pharmacy Analytics

        // GET: api/admin/analytics/pharmacy
        [HttpGet("pharmacy")]
        public async Task<IActionResult> GetPharmacyAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetPharmacyAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPharmacyAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/pharmacy/top-medicines
        [HttpGet("pharmacy/top-medicines")]
        public async Task<IActionResult> GetTopSellingMedicines([FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int top = 10
           )
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetTopSellingMedicinesAsync(top, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTopSellingMedicines");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Laboratory Analytics

        // GET: api/admin/analytics/laboratory
        [HttpGet("laboratory")]
        public async Task<IActionResult> GetLaboratoryAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetLaboratoryAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLaboratoryAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/laboratory/top-tests
        [HttpGet("laboratory/top-tests")]
        public async Task<IActionResult> GetMostRequestedTests([FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int top = 10
            )
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetMostRequestedTestsAsync(top, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMostRequestedTests");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Insurance Analytics

        // GET: api/admin/analytics/insurance
        [HttpGet("insurance")]
        public async Task<IActionResult> GetInsuranceAnalytics(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetInsuranceAnalyticsAsync(parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInsuranceAnalytics");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Geographical Analytics

        // GET: api/admin/analytics/geographical
        [HttpGet("geographical")]
        public async Task<IActionResult> GetGeographicalDistribution()
        {
            try
            {
                var result = await _adminAnalyticsService.GetGeographicalDistributionAsync();
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGeographicalDistribution");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/cities/{id}
        [HttpGet("cities/{id}")]
        public async Task<IActionResult> GetCityAnalytics(int id,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd))
                    parsedStartDate = sd;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed))
                    parsedEndDate = ed;

                var result = await _adminAnalyticsService.GetCityAnalyticsAsync(id, parsedStartDate, parsedEndDate);
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCityAnalytics for ID {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region System Health

        // GET: api/admin/analytics/system-health
        [HttpGet("system-health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var result = await _adminAnalyticsService.GetSystemHealthAsync();
                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSystemHealth");
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Export Data

        // POST: api/admin/analytics/export
        [HttpPost("export")]
        public async Task<IActionResult> ExportAnalyticsData([FromBody] ExportRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ReportType))
                    return BadRequest(ApiResponseHelper.Error("يجب تحديد نوع التقرير"));

                var result = await _adminAnalyticsService.ExportAnalyticsDataAsync(
                    request.ReportType, request.StartDate, request.EndDate);

                return StatusCode(GetStatusCode(result), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportAnalyticsData for report type {ReportType}", request?.ReportType);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        // GET: api/admin/analytics/reports/{reportId}/download
        [HttpGet("reports/{reportId}/download")]
        public async Task<IActionResult> DownloadReport(string reportId)
        {
            try
            {
                // In a real implementation, you would retrieve the report from storage
                // and return it as a file download
                // For now, return a placeholder response
                return NotFound(ApiResponseHelper.Error("التقرير غير موجود أو انتهت صلاحيته"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report {ReportId}", reportId);
                return StatusCode(500, ApiResponseHelper.InternalError());
            }
        }

        #endregion

        #region Helper Methods

        private int GetStatusCode(ApiResponse response)
        {
            if (response.Success)
                return 200;

            return 400;
        }

        #endregion
    }

    // Request Models
    public class ExportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}