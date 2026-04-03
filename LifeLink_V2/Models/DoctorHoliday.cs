using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class DoctorHoliday
{
    public int HolidayId { get; set; }

    public int DoctorId { get; set; }

    public DateOnly HolidayDate { get; set; }

    public string? HolidayName { get; set; }

    public bool IsAnnual { get; set; }

    public int? Year { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual ProviderDoctor Doctor { get; set; } = null!;
}
