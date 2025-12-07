using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Nursing;

public class ExecutionTask : EntityBase<long>
{
    public long MedicalOrderId { get; set; } // 来源医嘱
    [ForeignKey("MedicalOrderId")]
    public MedicalOrder MedicalOrder { get; set; }= null!;

    public string PatientId { get; set; } = null!; // 冗余方便查询
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;
    // 理论与实际
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    // 执行人
    public string? ExecutorStaffId { get; set; } // 实际扫码的人
    [ForeignKey("ExecutorStaffId")]
    public Nurse? Executor { get; set; }

    // 状态
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Skipped
    public bool IsRolledBack { get; set; } = false; // 是否已回滚
    
    // 动态数据 (比如皮试结果)
    public string DataPayload { get; set; } = string.Empty;
    public string ExceptionReason { get; set; } = string.Empty;
}