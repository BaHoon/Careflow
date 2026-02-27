using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class NursingCareNote
{
    public long Id { get; set; }

    public long? NursingTaskId { get; set; }

    public string PatientId { get; set; } = null!;

    public string RecorderNurseId { get; set; } = null!;

    public DateTime RecordTime { get; set; }

    public string Consciousness { get; set; } = null!;

    public string PupilLeft { get; set; } = null!;

    public string PupilRight { get; set; } = null!;

    public string SkinCondition { get; set; } = null!;

    public string PipeCareData { get; set; } = null!;

    public decimal IntakeVolume { get; set; }

    public string IntakeType { get; set; } = null!;

    public decimal OutputVolume { get; set; }

    public string OutputType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string HealthEducation { get; set; } = null!;

    public bool IsAmended { get; set; }

    public long? ParentAmendId { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Nurse RecorderNurse { get; set; } = null!;
}
