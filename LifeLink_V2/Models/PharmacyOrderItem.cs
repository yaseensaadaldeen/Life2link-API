using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class PharmacyOrderItem
{
    public int PharmacyOrderItemId { get; set; }

    public int PharmacyOrderId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceSyp { get; set; }

    public decimal? UnitPriceUsd { get; set; }

    public decimal LineTotalSyp { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual PharmacyOrder PharmacyOrder { get; set; } = null!;
    public string? Instructions { get; internal set; }
}
