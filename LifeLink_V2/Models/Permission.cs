using System;
using System.Collections.Generic;

namespace LifeLink_V2.Models;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string PermissionKey { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
