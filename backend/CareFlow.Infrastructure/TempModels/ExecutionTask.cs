using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class ExecutionTask
{
    public long Id { get; set; }

    public long MedicalOrderId { get; set; }

    public string PatientId { get; set; } = null!;

    public int Category { get; set; }

    public DateTime PlannedStartTime { get; set; }

    public string? AssignedNurseId { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public string? ExecutorStaffId { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public string? CompleterNurseId { get; set; }

    public int Status { get; set; }

    public int? StatusBeforeLocking { get; set; }

    public bool IsRolledBack { get; set; }

    public string DataPayload { get; set; } = null!;

    public string? ResultPayload { get; set; }

    public string ExceptionReason { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual Nurse? AssignedNurse { get; set; }

    public virtual Nurse? CompleterNurse { get; set; }

    public virtual Nurse? ExecutorStaff { get; set; }

    public virtual MedicalOrder MedicalOrder { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
