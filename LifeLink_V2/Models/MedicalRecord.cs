using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class MedicalRecord
{
    public int MedicalRecordId { get; set; }

    public int PatientId { get; set; }

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public DateTime RecordedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<MedicalRecordMedFile> MedicalRecordMedFiles { get; set; } = new List<MedicalRecordMedFile>();

    public virtual Patient Patient { get; set; } = null!;
}
