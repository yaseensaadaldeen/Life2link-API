using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabTest
{
    internal object PreparationInstructions;
    internal object SampleType;
    internal object TurnaroundTime;

    public int LabTestId { get; set; }

    public int ProviderId { get; set; }

    public string? TestCode { get; set; }

    public string TestName { get; set; } = null!;

    public int? CategoryId { get; set; }

    public decimal PriceSyp { get; set; }

    public decimal? PriceUsd { get; set; }

    public int? DurationMinutes { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual LabTestCategory? Category { get; set; }

    public virtual ICollection<LabOrderItem> LabOrderItems { get; set; } = new List<LabOrderItem>();

    public virtual ICollection<LabTestOrder> LabTestOrders { get; set; } = new List<LabTestOrder>();

    public virtual Provider Provider { get; set; } = null!;
}
