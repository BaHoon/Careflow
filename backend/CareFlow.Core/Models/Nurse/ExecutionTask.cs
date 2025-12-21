using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models.Nursing;

public class ExecutionTask : EntityBase<long>
{
    public long MedicalOrderId { get; set; } // 来源医嘱
    [ForeignKey("MedicalOrderId")]
    public MedicalOrder MedicalOrder { get; set; }= null!;

    public string PatientId { get; set; } = null!; // 冗余方便查询
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    /// <summary>
    /// 任务类别：决定了前端的交互流程和后台的状态流转逻辑 (Immediate, Duration, ResultPending, etc.)
    /// </summary>
    public TaskCategory Category { get; set; }

    // --- 时间与执行人 (双人双时) ---

    // 1. 计划时间
    public DateTime PlannedStartTime { get; set; }

    // 2. 开始执行 (对于所有类别：扫码时刻)
    public DateTime? ActualStartTime { get; set; }
    /// <summary>
    /// 发起护士 (扫码开始的人)
    /// 对于即刻类(Immediate)，也是完成人
    /// </summary>
    public string? ExecutorStaffId { get; set; } // 实际扫码的人
    [ForeignKey("ExecutorStaffId")]
    public Nurse? Executor { get; set; }

    // 3. 结束/完成执行 (对于持续类、结果类、记录类)
    public DateTime? ActualEndTime { get; set; }
    /// <summary>
    /// 结束护士 (点击结束/录入结果的人)
    /// 适用于：Duration(拔针), ResultPending(录结果), DataCollection(提交), Verification(核对完)
    /// </summary>
    public string? CompleterNurseId { get; set; }
    [ForeignKey("CompleterNurseId")]
    public Nurse? CompleterNurse { get; set; }

    // 状态
    public ExecutionTaskStatus Status { get; set; } = ExecutionTaskStatus.Pending;
    public bool IsRolledBack { get; set; } = false; // 是否已回滚
    
    /// <summary>
    /// [输入] 任务上下文/定义数据 (由系统生成任务时写入)
    /// 例子：TaskType, Title, SupplyList 等
    /// </summary>
    public string DataPayload { get; set; } = string.Empty;
    
    /// <summary>
    /// [输出] 任务执行结果 (由护士操作后写入)
    /// 用途：记录“测量的数值”、“实际扫描的条码”、“皮试结果”
    /// 初始为空，Task完成时必填（取决于业务逻辑）
    /// </summary>
    public string? ResultPayload { get; set; }

    public string ExceptionReason { get; set; } = string.Empty;
    
    // 审计字段
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}