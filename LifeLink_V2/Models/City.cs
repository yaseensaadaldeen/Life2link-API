using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class City
{
    public int CityId { get; set; }

    public int GovernorateId { get; set; }

    public string CityName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Governorate Governorate { get; set; } = null!;

    public virtual ICollection<Provider> Providers { get; set; } = new List<Provider>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
