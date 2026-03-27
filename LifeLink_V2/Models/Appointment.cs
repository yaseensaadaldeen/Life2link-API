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

    public virtual ICollection<AppointmentMedFile> AppointmentMedFiles { get; set; } = new List<AppointmentMedFile>();

    public virtual ProviderDoctor? Doctor { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;

    public virtual MedicalSpecialty? Specialty { get; set; }

    public virtual AppointmentStatus Status { get; set; } = null!;
    public string Notes { get; internal set; }
}
