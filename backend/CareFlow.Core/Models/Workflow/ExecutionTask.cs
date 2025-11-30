using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class ExecutionTask : EntityBase<Guid>
{
    public Guid OrderId { get; set; }
    public TaskType Type { get; set; }
    public ExecutionTaskStatus Status { get; set; }
    public Guid? PerformerNurseId { get; set; } // 扫码护士
    public DateTime? ExecutionTime { get; set; }

    public virtual MedicalOrder Order { get; set; } = null!;
    public virtual Nurse? PerformerNurse { get; set; }
}