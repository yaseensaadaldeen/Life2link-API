using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PrescriptionItem
{
    public int PrescriptionItemId { get; set; }

    public int PrescriptionId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public string Dosage { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public int? Duration { get; set; }

    public string? DurationUnit { get; set; }

    public string? Instructions { get; set; }

    public decimal? UnitPriceSyp { get; set; }

    public decimal? UnitPriceUsd { get; set; }

    public decimal? LineTotalSyp { get; set; }

    public bool IsSubstituted { get; set; }

    public int? SubstitutedMedicineId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Prescription Prescription { get; set; } = null!;

    public virtual Medicine? SubstitutedMedicine { get; set; }
}
