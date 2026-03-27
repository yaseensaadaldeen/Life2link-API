using LifeLink_V2.Helpers;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IAdminAnalyticsService
    {
        // Platform Overview
        Task<ApiResponse> GetPlatformOverviewAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Users Analytics
        Task<ApiResponse> GetUsersAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetUsersGrowthAsync(string period = "monthly"); // daily, weekly, monthly

        // Appointments Analytics
        Task<ApiResponse> GetAppointmentsAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetAppointmentsBySpecialtyAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetAppointmentsByProviderTypeAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Financial Analytics
        Task<ApiResponse> GetRevenueAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetRevenueBySourceAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetPaymentMethodsAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Providers Analytics
        Task<ApiResponse> GetProvidersAnalyticsAsync();
        Task<ApiResponse> GetTopProvidersAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetProviderPerformanceAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null);

        // Pharmacy Analytics
        Task<ApiResponse> GetPharmacyAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetTopSellingMedicinesAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null);

        // Laboratory Analytics
        Task<ApiResponse> GetLaboratoryAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse> GetMostRequestedTestsAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null);

        // Insurance Analytics
        Task<ApiResponse> GetInsuranceAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Geographical Analytics
        Task<ApiResponse> GetGeographicalDistributionAsync();
        Task<ApiResponse> GetCityAnalyticsAsync(int cityId, DateTime? startDate = null, DateTime? endDate = null);

        // System Health
        Task<ApiResponse> GetSystemHealthAsync();

        // Export Data
        Task<ApiResponse> ExportAnalyticsDataAsync(string reportType, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class AnalyticsFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? ProviderId { get; set; }
        public int? CityId { get; set; }
        public string? ProviderType { get; set; }
        public string? Specialty { get; set; }
    }
}