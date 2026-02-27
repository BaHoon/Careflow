using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class MedicationOrder
{
    public long Id { get; set; }

    public int UsageRoute { get; set; }

    public bool IsDynamicUsage { get; set; }

    public decimal? IntervalHours { get; set; }

    public DateTime? StartTime { get; set; }

    public string TimingStrategy { get; set; } = null!;

    public int SmartSlotsMask { get; set; }

    public int IntervalDays { get; set; }

    public virtual MedicalOrder IdNavigation { get; set; } = null!;
}
