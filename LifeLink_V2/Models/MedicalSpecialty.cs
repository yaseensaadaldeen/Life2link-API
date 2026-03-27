using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class MedicalSpecialty
{
    public int SpecialtyId { get; set; }

    public string SpecialtyName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<ProviderDoctor> ProviderDoctors { get; set; } = new List<ProviderDoctor>();
}
