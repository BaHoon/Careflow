using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class MedicalOrder
{
    public long Id { get; set; }

    public string PatientId { get; set; } = null!;

    public string DoctorId { get; set; } = null!;

    public string? NurseId { get; set; }

    public DateTime PlantEndTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string OrderType { get; set; } = null!;

    public int Status { get; set; }

    public bool IsLongTerm { get; set; }

    public string? Remarks { get; set; }

    public string? SignedByNurseId { get; set; }

    public DateTime? SignedAt { get; set; }

    public string? RejectReason { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? RejectedByNurseId { get; set; }

    public string? StopReason { get; set; }

    public DateTime? StopOrderTime { get; set; }

    public string? StopDoctorId { get; set; }

    public DateTime? StopConfirmedAt { get; set; }

    public string? StopConfirmedByNurseId { get; set; }

    public string? StopRejectReason { get; set; }

    public DateTime? StopRejectedAt { get; set; }

    public string? StopRejectedByNurseId { get; set; }

    public string? CancelReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledByDoctorId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CompletionType { get; set; }

    public DateTime? ResubmittedAt { get; set; }

    public string? ModificationNotes { get; set; }

    public DateTime CreateTime { get; set; }

    public virtual Doctor? CancelledByDoctor { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual ICollection<ExecutionTask> ExecutionTasks { get; set; } = new List<ExecutionTask>();

    public virtual InspectionOrder? InspectionOrder { get; set; }

    public virtual ICollection<MedicalOrderStatusHistory> MedicalOrderStatusHistories { get; set; } = new List<MedicalOrderStatusHistory>();

    public virtual MedicationOrder? MedicationOrder { get; set; }

    public virtual ICollection<MedicationOrderItem> MedicationOrderItems { get; set; } = new List<MedicationOrderItem>();

    public virtual Nurse? Nurse { get; set; }

    public virtual OperationOrder? OperationOrder { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Nurse? RejectedByNurse { get; set; }

    public virtual Nurse? SignedByNurse { get; set; }

    public virtual Nurse? StopConfirmedByNurse { get; set; }

    public virtual Doctor? StopDoctor { get; set; }

    public virtual Nurse? StopRejectedByNurse { get; set; }

    public virtual SurgicalOrder? SurgicalOrder { get; set; }
}
