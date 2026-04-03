using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class HealthRecord
{
    public int HealthRecordId { get; set; }

    public int PatientId { get; set; }

    public string RecordType { get; set; } = null!;

    public DateTime RecordDate { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Value { get; set; }

    public string? Unit { get; set; }

    public int? PerformedBy { get; set; }

    public string? Facility { get; set; }

    public int? DocumentFileId { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual MedFile? DocumentFile { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual ProviderDoctor? PerformedByNavigation { get; set; }
}
