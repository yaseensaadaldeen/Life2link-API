using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class ProviderDoctor
{
    public int DoctorId { get; set; }

    public int ProviderId { get; set; }

    public string FullName { get; set; } = null!;

    public int? SpecialtyId { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? WorkingHours { get; set; }

    public string? MedicalLicenseNumber { get; set; }

    public DateOnly? LicenseExpiry { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<DoctorAvailability> DoctorAvailabilities { get; set; } = new List<DoctorAvailability>();

    public virtual ICollection<DoctorBlockedSlot> DoctorBlockedSlots { get; set; } = new List<DoctorBlockedSlot>();

    public virtual ICollection<DoctorHoliday> DoctorHolidays { get; set; } = new List<DoctorHoliday>();

    public virtual ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual Provider Provider { get; set; } = null!;

    public virtual MedicalSpecialty? Specialty { get; set; }
}
