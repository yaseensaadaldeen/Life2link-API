using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Laboratory
{
    public class LabBookingRequestDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int LabId { get; set; }

        [Required]
        public int TestId { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; } = 30;
        public string? Notes { get; set; }
    }
}