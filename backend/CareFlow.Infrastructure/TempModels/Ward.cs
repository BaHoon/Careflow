using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Ward
{
    public string Id { get; set; } = null!;

    public string DepartmentId { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<NurseRoster> NurseRosters { get; set; } = new List<NurseRoster>();
}
