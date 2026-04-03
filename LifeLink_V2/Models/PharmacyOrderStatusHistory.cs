using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PharmacyOrderStatusHistory
{
    public int StatusHistoryId { get; set; }

    public int PharmacyOrderId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ChangeReason { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual PharmacyOrder PharmacyOrder { get; set; } = null!;
}
