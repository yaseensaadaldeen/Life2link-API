using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class ProviderType
{
    public int ProviderTypeId { get; set; }

    public string ProviderTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Provider> Providers { get; set; } = new List<Provider>();
}
