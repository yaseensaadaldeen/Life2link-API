using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class MedicalCondition
{
    public int ConditionId { get; set; }

    public int PatientId { get; set; }

    public string ConditionName { get; set; } = null!;

    public string? Icd10code { get; set; }

    public DateOnly? DiagnosisDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
