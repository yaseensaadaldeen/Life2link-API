using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class ClinicWorkingHour
{
    public int WorkingHourId { get; set; }

    public int ProviderId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsLunchBreak { get; set; }

    public TimeOnly? BreakStartTime { get; set; }

    public TimeOnly? BreakEndTime { get; set; }

    public bool IsActive { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
