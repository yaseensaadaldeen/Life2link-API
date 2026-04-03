using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int PatientId { get; set; }

    public int ProviderId { get; set; }

    public int? DoctorId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;
}
