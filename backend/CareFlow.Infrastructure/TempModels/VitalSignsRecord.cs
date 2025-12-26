using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class VitalSignsRecord
{
    public long Id { get; set; }

    public long? NursingTaskId { get; set; }

    public string PatientId { get; set; } = null!;

    public string RecorderNurseId { get; set; } = null!;

    public DateTime RecordTime { get; set; }

    public decimal Temperature { get; set; }

    public string TempType { get; set; } = null!;

    public int Pulse { get; set; }

    public int Respiration { get; set; }

    public int SysBp { get; set; }

    public int DiaBp { get; set; }

    public decimal Spo2 { get; set; }

    public int PainScore { get; set; }

    public decimal Weight { get; set; }

    public string Intervention { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Nurse RecorderNurse { get; set; } = null!;
}
