using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class MedicalRecordMedFile
{
    public int MedicalRecordMedFileId { get; set; }

    public int MedicalRecordId { get; set; }

    public int MedFileId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MedFile MedFile { get; set; } = null!;

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
}
