using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class ActivityLog
{
    public int ActivityLogId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Entity { get; set; }

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
