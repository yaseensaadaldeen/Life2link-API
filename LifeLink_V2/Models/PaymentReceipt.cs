using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PaymentReceipt
{
    public int ReceiptId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public int PaymentId { get; set; }

    public DateTime IssuedAt { get; set; }

    public int? IssuedBy { get; set; }

    public bool PrintedCopy { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
