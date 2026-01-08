using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 任务摘要DTO
/// 用于在医嘱详情中展示关联的执行任务
/// </summary>
public class TaskSummaryDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    public ExecutionTaskStatus Status { get; set; }
    
    /// <summary>
    /// 计划开始时间（UTC）
    /// </summary>
    public DateTime PlannedStartTime { get; set; }
    
    /// <summary>
    /// 实际开始时间（UTC）
    /// </summary>
    public DateTime? ActualStartTime { get; set; }
    
    /// <summary>
    /// 实际结束时间（UTC）
    /// </summary>
    public DateTime? ActualEndTime { get; set; }
    
    /// <summary>
    /// 任务类别
    /// </summary>
    public TaskCategory Category { get; set; }
    
    /// <summary>
    /// 理论执行护士ID（任务分配时指定的负责人）
    /// </summary>
    public string? AssignedNurseId { get; set; }
    
    /// <summary>
    /// 理论执行护士姓名
    /// </summary>
    public string? AssignedNurseName { get; set; }
    
    /// <summary>
    /// 执行护士ID（实际扫码/执行的人）
    /// </summary>
    public string? ExecutorStaffId { get; set; }
    
    /// <summary>
    /// 执行护士姓名（实际开始执行的人）
    /// </summary>
    public string? ExecutorName { get; set; }
    
    /// <summary>
    /// 结束护士ID（实际结束任务的人）
    /// </summary>
    public string? CompleterNurseId { get; set; }
    
    /// <summary>
    /// 结束护士姓名（实际结束任务的人）
    /// </summary>
    public string? CompleterNurseName { get; set; }
    
    /// <summary>
    /// 任务被锁定前的原始状态（仅当status为OrderStopping时有值）
    /// </summary>
    public ExecutionTaskStatus? StatusBeforeLocking { get; set; }
    
    /// <summary>
    /// 异常原因（如果任务状态为Incomplete）
    /// </summary>
    public string? ExceptionReason { get; set; }
    
    /// <summary>
    /// 任务数据载荷（JSON格式，包含Title、Description等任务详细信息）
    /// </summary>
    public string? DataPayload { get; set; }
    
    /// <summary>
    /// 执行结果（仅ResultPending类任务有效）
    /// 记录测量数值、检测数据、皮试结果等
    /// </summary>
    public string? ResultPayload { get; set; }
    
    /// <summary>
    /// 执行备注（所有任务类型都可填写）
    /// 记录执行过程中的备注、观察、特殊情况说明
    /// </summary>
    public string? ExecutionRemarks { get; set; }
}
