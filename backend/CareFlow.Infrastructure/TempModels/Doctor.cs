using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Doctor
{
    public string Id { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string PrescriptionAuthLevel { get; set; } = null!;

    public virtual Staff IdNavigation { get; set; } = null!;

    public virtual ICollection<InspectionReport> InspectionReports { get; set; } = new List<InspectionReport>();

    public virtual ICollection<MedicalOrder> MedicalOrderCancelledByDoctors { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<MedicalOrder> MedicalOrderDoctors { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<MedicalOrder> MedicalOrderStopDoctors { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
