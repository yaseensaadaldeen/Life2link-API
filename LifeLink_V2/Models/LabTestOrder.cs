using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabTestOrder
{
    internal bool IsHomeCollection;
    internal object HomeCollectionAddress;
    internal object HomeCollectionPhone;
    internal object DoctorInstructions;
    internal object Notes;
    internal int LabTestOrderId;

    public int LabOrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int PatientId { get; set; }

    public int ProviderId { get; set; }

    public int TestId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public string Status { get; set; } = null!;

    public decimal PriceSyp { get; set; }

    public decimal? PriceUsd { get; set; }

    public bool IsPaid { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CollectedBy { get; set; }

    public DateTime? CollectedAt { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? LabNotes { get; set; }

    public bool Urgent { get; set; }

    public string? SampleType { get; set; }

    public DateTime? SampleCollectedAt { get; set; }

    public virtual ICollection<LabOrderItem> LabOrderItems { get; set; } = new List<LabOrderItem>();

    public virtual ICollection<LabOrderStatusHistory> LabOrderStatusHistories { get; set; } = new List<LabOrderStatusHistory>();

    public virtual LabTestResult? LabTestResult { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;

    public virtual LabTest Test { get; set; } = null!;
}
