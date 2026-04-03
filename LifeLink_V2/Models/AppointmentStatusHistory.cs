using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class AppointmentStatusHistory
{
    public int StatusHistoryId { get; set; }

    public int AppointmentId { get; set; }

    public int? OldStatusId { get; set; }

    public int NewStatusId { get; set; }

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ChangeReason { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual AppointmentStatus NewStatus { get; set; } = null!;

    public virtual AppointmentStatus? OldStatus { get; set; }
}
