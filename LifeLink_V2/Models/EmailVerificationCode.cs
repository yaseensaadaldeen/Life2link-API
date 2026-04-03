using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class EmailVerificationCode
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime Expiry { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
