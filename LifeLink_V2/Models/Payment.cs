using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public string? PaymentRef { get; set; }

    public int? AppointmentId { get; set; }

    public int? PharmacyOrderId { get; set; }

    public int? LabOrderId { get; set; }

    public int PatientId { get; set; }

    public int? ProviderId { get; set; }

    public int PaymentMethodId { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public decimal AmountSyp { get; set; }

    public decimal? AmountUsd { get; set; }

    public decimal? InsuranceCoverageAmountSyp { get; set; }

    public decimal PatientPayAmountSyp { get; set; }

    public decimal? ExchangeRate { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();

    public virtual Provider? Provider { get; set; }
}
