using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class InsuranceClaim
{
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

    public virtual InsuranceCompany InsuranceCompany { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Payment? RelatedPayment { get; set; }
    public TimeSpan? UpdatedAt { get; internal set; }
}
