using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabTestOrder
{
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

    public virtual LabTestResult? LabTestResult { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;

    public virtual LabTest Test { get; set; } = null!;

    // Changed from 'object' and non-public setters to concrete nullable strings with public setters.
    // EF Core can map these directly as nvarchar columns.
    public bool IsHomeCollection { get; set; }
    public string? HomeCollectionAddress { get; set; }
    public string? HomeCollectionPhone { get; set; }
    public string? DoctorInstructions { get; set; }
    public string? Notes { get; set; }
}