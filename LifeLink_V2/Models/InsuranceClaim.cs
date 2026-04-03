using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class InsuranceClaim
{
    internal DateTime? UpdatedAt;

    public int ClaimId { get; set; }

    public string ClaimCode { get; set; } = null!;

    public int PatientId { get; set; }

    public int InsuranceCompanyId { get; set; }

    public int? RelatedPaymentId { get; set; }

    public decimal ClaimAmountSyp { get; set; }

    public decimal? ApprovedAmountSyp { get; set; }

    public string Status { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int? ProcessedBy { get; set; }

    public virtual ICollection<ClaimStatusHistory> ClaimStatusHistories { get; set; } = new List<ClaimStatusHistory>();

    public virtual InsuranceCompany InsuranceCompany { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Payment? RelatedPayment { get; set; }
}
