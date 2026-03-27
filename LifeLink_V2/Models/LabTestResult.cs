using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabTestResult
{
    public int LabTestResultId { get; set; }

    public int LabOrderId { get; set; }

    public string? ResultSummary { get; set; }

    public int? FullReportMedFileId { get; set; }

    public string? ResultData { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual MedFile? FullReportMedFile { get; set; }

    public virtual LabTestOrder LabOrder { get; set; } = null!;
    public string NormalRanges { get; internal set; }
    public string? ResultDataJson { get; internal set; }
    public string? Interpretations { get; internal set; }
    public string? TechnicianNotes { get; internal set; }
    public string? VerifiedBy { get; internal set; }
}
