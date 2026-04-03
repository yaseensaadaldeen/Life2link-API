using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PatientAllergy
{
    public int AllergyId { get; set; }

    public int PatientId { get; set; }

    public string Allergen { get; set; } = null!;

    public string? Reaction { get; set; }

    public string Severity { get; set; } = null!;

    public DateOnly? DiagnosisDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
