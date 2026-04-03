using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class RefreshToken
{
    public int TokenId { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public byte[]? TokenHash { get; set; }

    public virtual User User { get; set; } = null!;
}
