using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class VwDoctorAvailability
{
    public int AvailabilityId { get; set; }

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = null!;

    public int? SpecialtyId { get; set; }

    public string SpecialtyName { get; set; } = null!;

    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int SlotDurationMinutes { get; set; }

    public bool IsActive { get; set; }

    public int HasAppointments { get; set; }
}
