using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Governorate
{
    public int GovernorateId { get; set; }

    public string GovernorateName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<City> Cities { get; set; } = new List<City>();
}
