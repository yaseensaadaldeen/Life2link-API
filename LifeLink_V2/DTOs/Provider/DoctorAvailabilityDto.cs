using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Provider
{
    public class CreateDoctorAvailabilityDto
    {
        [Required]
        public int DayOfWeek { get; set; } // 0 = Sunday .. 6 = Saturday

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [Range(5, 240)]
        public int SlotDurationMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateDoctorAvailabilityDto
    {
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        [Range(5, 240)]
        public int? SlotDurationMinutes { get; set; }
        public bool? IsActive { get; set; }
    }

    public class DoctorAvailabilitySlotDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAvailable { get; set; }
    }
}