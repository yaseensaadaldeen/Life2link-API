using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public int ProviderId { get; set; }

    public string MedicineName { get; set; } = null!;

    public string? Dosage { get; set; }

    public decimal PriceSyp { get; set; }

    public decimal? PriceUsd { get; set; }

    public int QuantityInStock { get; set; }

    public int LowStockThreshold { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<PharmacyOrderItem> PharmacyOrderItems { get; set; } = new List<PharmacyOrderItem>();

    public virtual ICollection<PrescriptionItem> PrescriptionItemMedicines { get; set; } = new List<PrescriptionItem>();

    public virtual ICollection<PrescriptionItem> PrescriptionItemSubstitutedMedicines { get; set; } = new List<PrescriptionItem>();

    public virtual Provider Provider { get; set; } = null!;
}
