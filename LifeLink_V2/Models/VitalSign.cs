using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class VitalSign
{
    public int VitalSignId { get; set; }

    public int PatientId { get; set; }

    public DateTime RecordedAt { get; set; }

    public int RecordedBy { get; set; }

    public int? BloodPressureSystolic { get; set; }

    public int? BloodPressureDiastolic { get; set; }

    public int? HeartRate { get; set; }

    public int? RespiratoryRate { get; set; }

    public decimal? Temperature { get; set; }

    public int? OxygenSaturation { get; set; }

    public decimal? BloodGlucose { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }

    public decimal? Bmi { get; set; }

    public string? Notes { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual User RecordedByNavigation { get; set; } = null!;
}
