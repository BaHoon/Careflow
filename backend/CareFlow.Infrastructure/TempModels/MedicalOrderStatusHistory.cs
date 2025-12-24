using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class MedicalOrderStatusHistory
{
    public long Id { get; set; }

    public long MedicalOrderId { get; set; }

    public int FromStatus { get; set; }

    public int ToStatus { get; set; }

    public DateTime ChangedAt { get; set; }

    public string ChangedById { get; set; } = null!;

    public string ChangedByType { get; set; } = null!;

    public string? ChangedByName { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual MedicalOrder MedicalOrder { get; set; } = null!;
}
