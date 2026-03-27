using LifeLink_V2.Helpers;
using LifeLink_V2.Models;

namespace LifeLink_V2.Services.Interfaces
{
    public interface ILaboratoryService
    {
        // Lab Tests Management
        Task<ApiResponse> GetLabTestsAsync(int providerId, int? categoryId = null, string? search = null, int page = 1, int pageSize = 20);
        Task<ApiResponse> GetLabTestByIdAsync(int testId);
        Task<ApiResponse> AddLabTestAsync(AddLabTestDto testDto, int currentUserId);
        Task<ApiResponse> UpdateLabTestAsync(int testId, UpdateLabTestDto testDto, int currentUserId);
        Task<ApiResponse> DeleteLabTestAsync(int testId, int currentUserId);

        // Lab Test Categories
        Task<ApiResponse> GetLabTestCategoriesAsync();
        Task<ApiResponse> AddLabTestCategoryAsync(AddLabTestCategoryDto categoryDto, int currentUserId);

        // Lab Orders
        Task<ApiResponse> GetLabOrdersAsync(int? patientId = null, int? providerId = null,
            string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20);
        Task<ApiResponse> GetLabOrderByIdAsync(int orderId);
        Task<ApiResponse> CreateLabOrderAsync(CreateLabOrderDto orderDto, int currentUserId);
        Task<ApiResponse> UpdateLabOrderStatusAsync(int orderId, string status, int currentUserId);
        Task<ApiResponse> CancelLabOrderAsync(int orderId, int currentUserId, string reason = null);
        Task<ApiResponse> CompleteLabOrderAsync(int orderId, int currentUserId);

        // Lab Results
        Task<ApiResponse> GetLabResultByOrderIdAsync(int orderId);
        Task<ApiResponse> SubmitLabResultAsync(int orderId, SubmitLabResultDto resultDto, int currentUserId);
        Task<ApiResponse> UpdateLabResultAsync(int resultId, UpdateLabResultDto resultDto, int currentUserId);

        // Patient Lab History
        Task<ApiResponse> GetPatientLabHistoryAsync(int patientId, int page = 1, int pageSize = 20);

        // Provider Dashboard
        Task<ApiResponse> GetLaboratoryDashboardAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class AddLabTestDto
    {
        public int ProviderId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? TestCode { get; set; }
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
        public decimal PriceSYP { get; set; }
        public decimal PriceUSD { get; set; }
        public string? PreparationInstructions { get; set; }
        public string? SampleType { get; set; }
        public string? TurnaroundTime { get; set; }
    }

    public class UpdateLabTestDto
    {
        public string? TestName { get; set; }
        public string? TestCode { get; set; }
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
        public decimal? PriceSYP { get; set; }
        public decimal? PriceUSD { get; set; }
        public string? PreparationInstructions { get; set; }
        public string? SampleType { get; set; }
        public string? TurnaroundTime { get; set; }
    }

    public class AddLabTestCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateLabOrderDto
    {
        public int PatientId { get; set; }
        public int ProviderId { get; set; }
        public int TestId { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public string? Notes { get; set; }
        public string? DoctorInstructions { get; set; }
        public bool IsHomeCollection { get; set; } = false;
        public string? HomeCollectionAddress { get; set; }
        public string? HomeCollectionPhone { get; set; }
    }

    public class SubmitLabResultDto
    {
        public string ResultSummary { get; set; } = string.Empty;
        public string? ResultDataJson { get; set; }
        public string? NormalRanges { get; set; }
        public string? Interpretations { get; set; }
        public int? FullReportMedFileId { get; set; }
        public string? TechnicianNotes { get; set; }
        public string? VerifiedBy { get; set; }
    }

    public class UpdateLabResultDto
    {
        public string? ResultSummary { get; set; }
        public string? ResultDataJson { get; set; }
        public string? NormalRanges { get; set; }
        public string? Interpretations { get; set; }
        public int? FullReportMedFileId { get; set; }
        public string? TechnicianNotes { get; set; }
        public string? VerifiedBy { get; set; }
    }
}