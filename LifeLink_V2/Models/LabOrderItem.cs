using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabOrderItem
{
    public int LabOrderItemId { get; set; }

    public int LabOrderId { get; set; }

    public int LabTestId { get; set; }

    public decimal? UnitPriceSyp { get; set; }

    public decimal? UnitPriceUsd { get; set; }

    public decimal? LineTotalSyp { get; set; }

    public string Status { get; set; } = null!;

    public string? ResultSummary { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? PerformedBy { get; set; }

    public DateTime? PerformedAt { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int? ResultFileId { get; set; }

    public string? ReferenceRange { get; set; }

    public string? Unit { get; set; }

    public bool IsAbnormal { get; set; }

    public virtual LabTestOrder LabOrder { get; set; } = null!;

    public virtual LabTest LabTest { get; set; } = null!;
}
