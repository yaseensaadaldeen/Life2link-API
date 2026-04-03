using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PharmacyOrder
{
    internal object IsDelivery;

    public int PharmacyOrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int PatientId { get; set; }

    public int ProviderId { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalSyp { get; set; }

    public decimal? TotalUsd { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? PharmacyNotes { get; set; }

    public int? DispensedBy { get; set; }

    public DateTime? DispensedAt { get; set; }

    public int? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PharmacyOrderItem> PharmacyOrderItems { get; set; } = new List<PharmacyOrderItem>();

    public virtual ICollection<PharmacyOrderStatusHistory> PharmacyOrderStatusHistories { get; set; } = new List<PharmacyOrderStatusHistory>();

    public virtual Provider Provider { get; set; } = null!;
}
