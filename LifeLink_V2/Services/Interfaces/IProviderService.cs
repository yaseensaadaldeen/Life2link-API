using LifeLink_V2.DTOs.Provider;
using LifeLink_V2.Helpers;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IProviderService
    {
        // Provider CRUD
        Task<ApiResponse> CreateProviderAsync(CreateProviderDto createDto, int createdBy);
        Task<ApiResponse> GetProviderByIdAsync(int providerId);
        Task<ApiResponse> GetProviderByUserIdAsync(int userId);
        Task<ApiResponse> UpdateProviderAsync(int providerId, UpdateProviderDto updateDto, int updatedBy);
        Task<ApiResponse> DeleteProviderAsync(int providerId, int deletedBy);
        Task<ApiResponse> SearchProvidersAsync(ProviderSearchDto searchDto, int page = 1, int pageSize = 20);
        Task<ApiResponse> GetProviderStatsAsync(int providerId);
        Task<ApiResponse> GetRecentProvidersAsync(int count = 10);
        Task<ApiResponse> UpdateProviderStatusAsync(int providerId, bool isActive, int updatedBy);

        // Doctor Management
        Task<ApiResponse> AddDoctorAsync(int providerId, CreateDoctorDto createDto, int createdBy);
        Task<ApiResponse> GetDoctorByIdAsync(int doctorId);
        Task<ApiResponse> GetDoctorsByProviderAsync(int providerId, bool? activeOnly = true);
        Task<ApiResponse> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDto, int updatedBy);
        Task<ApiResponse> DeleteDoctorAsync(int doctorId, int deletedBy);
        Task<ApiResponse> GetDoctorStatsAsync(int doctorId);
    }
}