using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class InspectionOrder
{
    public long Id { get; set; }

    public string ItemCode { get; set; } = null!;

    public string RisLisId { get; set; } = null!;

    public string Location { get; set; } = null!;

    public int Source { get; set; }

    public DateTime? AppointmentTime { get; set; }

    public string? AppointmentPlace { get; set; }

    public string? Precautions { get; set; }

    public DateTime? CheckStartTime { get; set; }

    public DateTime? CheckEndTime { get; set; }

    public DateTime? ReportPendingTime { get; set; }

    public DateTime? ReportTime { get; set; }

    public string? ReportId { get; set; }

    public int InspectionStatus { get; set; }

    public virtual MedicalOrder IdNavigation { get; set; } = null!;

    public virtual ICollection<InspectionReport> InspectionReports { get; set; } = new List<InspectionReport>();
}
