using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class SystemSetting
{
    public string SettingKey { get; set; } = null!;

    public string? SettingValue { get; set; }

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }
}
