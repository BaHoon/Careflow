using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class SurgicalOrder
{
    public long Id { get; set; }

    public string SurgeryName { get; set; } = null!;

    public DateTime ScheduleTime { get; set; }

    public string AnesthesiaType { get; set; } = null!;

    public string IncisionSite { get; set; } = null!;

    public string SurgeonId { get; set; } = null!;

    public string? RequiredTalk { get; set; }

    public string? RequiredOperation { get; set; }

    public float PrepProgress { get; set; }

    public string PrepStatus { get; set; } = null!;

    public virtual MedicalOrder IdNavigation { get; set; } = null!;
}
