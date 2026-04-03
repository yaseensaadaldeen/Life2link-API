using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public string PrescriptionCode { get; set; } = null!;

    public int PatientId { get; set; }

    public int ProviderId { get; set; }

    public int? DoctorId { get; set; }

    public int? AppointmentId { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public string? PrescriptionNumber { get; set; }

    public int? RefillsAllowed { get; set; }

    public int? RefillsRemaining { get; set; }

    public DateTime? DispensedAt { get; set; }

    public string? PharmacyNotes { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual ProviderDoctor? Doctor { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();

    public virtual Provider Provider { get; set; } = null!;
}
