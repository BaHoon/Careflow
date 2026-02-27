using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class ShiftType
{
    public string Id { get; set; } = null!;

    public string ShiftName { get; set; } = null!;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual ICollection<NurseRoster> NurseRosters { get; set; } = new List<NurseRoster>();
}
