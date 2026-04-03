using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class DoctorBlockedSlot
{
    public int BlockedSlotId { get; set; }

    public int DoctorId { get; set; }

    public DateOnly BlockedDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Reason { get; set; }

    public bool IsRecurring { get; set; }

    public string? RecurrencePattern { get; set; }

    public DateOnly? RecurrenceEndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual ProviderDoctor Doctor { get; set; } = null!;
}
