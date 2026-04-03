using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PatientSensitiveDatum
{
    public int PatientId { get; set; }

    public byte[]? NationalIdEncrypted { get; set; }

    public byte[]? InsuranceNumberEncrypted { get; set; }

    public byte[]? EmergencyContactEncrypted { get; set; }

    public byte[]? EmergencyPhoneEncrypted { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
