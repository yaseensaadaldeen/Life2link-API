using LifeLink_V2.Helpers;
using LifeLink_V2.DTOs.Patient;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IPatientService
    {
        Task<ApiResponse> CreatePatientAsync(CreatePatientDto createDto, int createdBy);
        Task<ApiResponse> GetPatientByIdAsync(int patientId);
        Task<ApiResponse> GetPatientByUserIdAsync(int userId);
        Task<ApiResponse> UpdatePatientAsync(int patientId, UpdatePatientDto updateDto, int updatedBy);
        Task<ApiResponse> DeletePatientAsync(int patientId, int deletedBy);
        Task<ApiResponse> SearchPatientsAsync(PatientSearchDto searchDto, int page = 1, int pageSize = 20);
        Task<ApiResponse> GetPatientStatsAsync(int patientId);
        Task<ApiResponse> GetRecentPatientsAsync(int count = 10);
    }
}