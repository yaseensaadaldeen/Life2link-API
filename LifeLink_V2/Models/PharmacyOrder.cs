using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PharmacyOrder
{
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

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PharmacyOrderItem> PharmacyOrderItems { get; set; } = new List<PharmacyOrderItem>();

    public virtual Provider Provider { get; set; } = null!;
    public string? Notes { get; internal set; }
    public string? IsDelivery { get; internal set; }
}
