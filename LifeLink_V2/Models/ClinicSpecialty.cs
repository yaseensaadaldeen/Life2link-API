using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class ClinicSpecialty
{
    public int ClinicSpecialtyId { get; set; }

    public int ProviderId { get; set; }

    public int SpecialtyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Provider Provider { get; set; } = null!;

    public virtual MedicalSpecialty Specialty { get; set; } = null!;
}
