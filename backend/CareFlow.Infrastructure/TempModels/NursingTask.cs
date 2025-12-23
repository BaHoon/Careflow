using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class NursingTask
{
    public long Id { get; set; }

    public string PatientId { get; set; } = null!;

    public DateTime ScheduledTime { get; set; }

    public string? AssignedNurseId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ExecuteTime { get; set; }

    public string? ExecutorNurseId { get; set; }

    public string TaskType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual Nurse? AssignedNurse { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
