using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class AppointmentMedFile
{
    public int AppointmentMedFileId { get; set; }

    public int AppointmentId { get; set; }

    public int MedFileId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual MedFile MedFile { get; set; } = null!;
}
