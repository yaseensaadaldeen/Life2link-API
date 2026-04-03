using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public bool IsRead { get; set; }

    public string? Channel { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? NotificationType { get; set; }

    public string? Message { get; set; }

    public int? Priority { get; set; }

    public string? ActionUrl { get; set; }

    public string? Metadata { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual User? User { get; set; }
}
