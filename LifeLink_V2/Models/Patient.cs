using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public int UserId { get; set; }

    public string? NationalId { get; set; }

    public int? InsuranceCompanyId { get; set; }

    public string? InsuranceNumber { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public string? BloodType { get; set; }

    public string? EmergencyContact { get; set; }

    public string? EmergencyPhone { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();

    public virtual InsuranceCompany? InsuranceCompany { get; set; }

    public virtual ICollection<LabTestOrder> LabTestOrders { get; set; } = new List<LabTestOrder>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PharmacyOrder> PharmacyOrders { get; set; } = new List<PharmacyOrder>();

    public virtual User User { get; set; } = null!;
}
