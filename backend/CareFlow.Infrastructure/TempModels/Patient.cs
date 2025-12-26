using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Patient
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string IdCard { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public int Age { get; set; }

    public float Weight { get; set; }

    public string Status { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public int NursingGrade { get; set; }

    public string BedId { get; set; } = null!;

    public string AttendingDoctorId { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual Doctor AttendingDoctor { get; set; } = null!;

    public virtual Bed Bed { get; set; } = null!;

    public virtual ICollection<ExecutionTask> ExecutionTasks { get; set; } = new List<ExecutionTask>();

    public virtual ICollection<InspectionReport> InspectionReports { get; set; } = new List<InspectionReport>();

    public virtual ICollection<MedicalOrder> MedicalOrders { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<NursingCareNote> NursingCareNotes { get; set; } = new List<NursingCareNote>();

    public virtual ICollection<NursingTask> NursingTasks { get; set; } = new List<NursingTask>();

    public virtual ICollection<VitalSignsRecord> VitalSignsRecords { get; set; } = new List<VitalSignsRecord>();
}
