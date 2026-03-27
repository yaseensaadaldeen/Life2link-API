using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class LabTestCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
}
