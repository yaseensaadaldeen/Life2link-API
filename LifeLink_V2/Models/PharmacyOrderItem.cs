using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PharmacyOrderItem
{
    internal object Instructions;

    public int PharmacyOrderItemId { get; set; }

    public int PharmacyOrderId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceSyp { get; set; }

    public decimal? UnitPriceUsd { get; set; }

    public decimal LineTotalSyp { get; set; }

    public bool IsDispensed { get; set; }

    public int? DispensedQuantity { get; set; }

    public DateTime? DispensedAt { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual PharmacyOrder PharmacyOrder { get; set; } = null!;
}
