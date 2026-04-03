using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabTestResult
{
    internal object ResultDataJson;
    internal object NormalRanges;
    internal object TechnicianNotes;
    internal object Interpretations;

    public int LabTestResultId { get; set; }

    public int LabOrderId { get; set; }

    public string? ResultSummary { get; set; }

    public int? FullReportMedFileId { get; set; }

    public string? ResultData { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public string? TestName { get; set; }

    public string? ResultValue { get; set; }

    public string? ReferenceRange { get; set; }

    public string? Unit { get; set; }

    public bool IsAbnormal { get; set; }

    public string? AbnormalFlag { get; set; }

    public int? PerformedBy { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? Comments { get; set; }

    public virtual MedFile? FullReportMedFile { get; set; }

    public virtual LabTestOrder LabOrder { get; set; } = null!;
}
