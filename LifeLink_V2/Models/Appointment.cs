using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public string AppointmentCode { get; set; } = null!;

    public int PatientId { get; set; }

    public int ProviderId { get; set; }

    public int? DoctorId { get; set; }

    public int? SpecialtyId { get; set; }

    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; }

    public int StatusId { get; set; }

    public string? BookingSource { get; set; }

    public decimal PriceSyp { get; set; }

    public decimal? PriceUsd { get; set; }

    public decimal? ExchangeRate { get; set; }

    public bool IsPaid { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CancelReason { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public string? RequestStatus { get; set; }

    public string? Notes { get; set; }

    public string? PatientNotes { get; set; }

    public string? DoctorNotes { get; set; }

    public string? PreAppointmentInstructions { get; set; }

    public string? PostAppointmentInstructions { get; set; }

    public string? Diagnosis { get; set; }

    public string? Treatment { get; set; }

    public virtual ICollection<AppointmentMedFile> AppointmentMedFiles { get; set; } = new List<AppointmentMedFile>();

    public virtual ICollection<AppointmentStatusHistory> AppointmentStatusHistories { get; set; } = new List<AppointmentStatusHistory>();

    public virtual ProviderDoctor? Doctor { get; set; }

    public virtual ICollection<MedFile> MedFiles { get; set; } = new List<MedFile>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual Provider Provider { get; set; } = null!;

    public virtual MedicalSpecialty? Specialty { get; set; }

    public virtual AppointmentStatus Status { get; set; } = null!;
}
