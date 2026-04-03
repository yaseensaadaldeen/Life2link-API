using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class DoctorAvailability
{
    public int AvailabilityId { get; set; }

    public int DoctorId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int SlotDurationMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ProviderDoctor Doctor { get; set; } = null!;
}
