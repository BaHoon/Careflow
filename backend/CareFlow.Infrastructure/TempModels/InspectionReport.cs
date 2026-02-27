using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class InspectionReport
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string PatientId { get; set; } = null!;

    public string RisLisId { get; set; } = null!;

    public DateTime ReportTime { get; set; }

    public int ReportStatus { get; set; }

    public string? Findings { get; set; }

    public string? Impression { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? ReviewerId { get; set; }

    public int ReportSource { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual InspectionOrder Order { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Doctor? Reviewer { get; set; }
}
