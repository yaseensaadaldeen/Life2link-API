using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class InsuranceCompany
{
    public int InsuranceCompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? Category { get; set; }

    public string? Country { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
