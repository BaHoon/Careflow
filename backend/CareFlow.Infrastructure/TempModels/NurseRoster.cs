using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class NurseRoster
{
    public long Id { get; set; }

    public string StaffId { get; set; } = null!;

    public string WardId { get; set; } = null!;

    public string ShiftId { get; set; } = null!;

    public DateOnly WorkDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual ShiftType Shift { get; set; } = null!;

    public virtual Nurse Staff { get; set; } = null!;

    public virtual Ward Ward { get; set; } = null!;
}
