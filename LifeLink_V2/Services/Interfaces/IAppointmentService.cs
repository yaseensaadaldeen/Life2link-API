using LifeLink_V2.Helpers;
using LifeLink_V2.Models;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<ApiResponse> GetAppointmentsAsync(int? patientId = null, int? providerId = null,
            int? statusId = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20);

        Task<ApiResponse> GetAppointmentByIdAsync(int appointmentId);

        Task<ApiResponse> CreateAppointmentAsync(CreateAppointmentDto appointmentDto, int currentUserId);

        Task<ApiResponse> UpdateAppointmentStatusAsync(int appointmentId, int statusId, int currentUserId);

        Task<ApiResponse> CancelAppointmentAsync(int appointmentId, int currentUserId, string reason = null);

        Task<ApiResponse> CompleteAppointmentAsync(int appointmentId, int currentUserId);

        Task<ApiResponse> GetProviderAvailabilityAsync(int providerId, int? doctorId, DateTime date);

        Task<ApiResponse> GetPatientAppointmentsAsync(int patientId, int page = 1, int pageSize = 20);

        Task<ApiResponse> GetProviderAppointmentsAsync(int providerId, int? statusId = null,
            DateTime? date = null, int page = 1, int pageSize = 20);

        Task<ApiResponse> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateDto, int currentUserId);

        Task<ApiResponse> AddMedFileToAppointmentAsync(int appointmentId, int medFileId, int currentUserId);
    }

    public class CreateAppointmentDto
    {
        public int PatientId { get; set; }
        public int ProviderId { get; set; }
        public int? DoctorId { get; set; }
        public int? SpecialtyId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public decimal PriceSYP { get; set; }
        public decimal PriceUSD { get; set; }
        public string? BookingSource { get; set; } = "Mobile";
        public string? Notes { get; set; }
    }

    public class UpdateAppointmentDto
    {
        public DateTime? ScheduledAt { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
    }
}