using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class HospitalTimeSlot
{
    public int Id { get; set; }

    public string SlotCode { get; set; } = null!;

    public string SlotName { get; set; } = null!;

    public TimeSpan DefaultTime { get; set; }

    public int OffsetMinutes { get; set; }

    public DateTime CreateTime { get; set; }
}
