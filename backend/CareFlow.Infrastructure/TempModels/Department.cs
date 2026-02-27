using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Department
{
    public string Id { get; set; } = null!;

    public string DeptName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
