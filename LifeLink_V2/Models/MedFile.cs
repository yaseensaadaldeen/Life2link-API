using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class MedFile
{
    public int MedFileId { get; set; }

    public string MedFileName { get; set; } = null!;

    public string MedFilePath { get; set; } = null!;

    public long? MedFileSize { get; set; }

    public string? ContentType { get; set; }

    public int? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsDeleted { get; set; }

    public int? PatientId { get; set; }

    public string? FileName { get; set; }

    public string? FileType { get; set; }

    public string? Category { get; set; }

    public int? AppointmentId { get; set; }

    public int? MedicalRecordId { get; set; }

    public bool IsArchived { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual ICollection<AppointmentMedFile> AppointmentMedFiles { get; set; } = new List<AppointmentMedFile>();

    public virtual ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();

    public virtual ICollection<LabTestResult> LabTestResults { get; set; } = new List<LabTestResult>();

    public virtual ICollection<MedicalRecordMedFile> MedicalRecordMedFiles { get; set; } = new List<MedicalRecordMedFile>();

    public virtual Patient? Patient { get; set; }

    public virtual User? UploadedByNavigation { get; set; }
}
