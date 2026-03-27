using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Provider
{
    public int ProviderId { get; set; }

    public int UserId { get; set; }

    public int ProviderTypeId { get; set; }

    public string ProviderName { get; set; } = null!;

    public int? CityId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public decimal? Rating { get; set; }

    public int? TotalAppointments { get; set; }

    public bool IsActive { get; set; }

    public string? MedicalLicenseNumber { get; set; }

    public string? LicenseIssuedBy { get; set; }

    public DateOnly? LicenseExpiry { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual City? City { get; set; }

    public virtual ICollection<LabTestOrder> LabTestOrders { get; set; } = new List<LabTestOrder>();

    public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PharmacyOrder> PharmacyOrders { get; set; } = new List<PharmacyOrder>();

    public virtual ICollection<ProviderDoctor> ProviderDoctors { get; set; } = new List<ProviderDoctor>();

    public virtual ProviderType ProviderType { get; set; } = null!;

    public virtual User User { get; set; } = null!;
    public string Description { get; internal set; }
}
