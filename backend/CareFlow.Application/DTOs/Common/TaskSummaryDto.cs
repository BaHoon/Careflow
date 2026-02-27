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
    /// 执行护士ID
    /// </summary>
    public string? ExecutorStaffId { get; set; }
    
    /// <summary>
    /// 执行护士姓名
    /// </summary>
    public string? ExecutorName { get; set; }
    
    /// <summary>
    /// 任务被锁定前的原始状态（仅当status为OrderStopping时有值）
    /// </summary>
    public ExecutionTaskStatus? StatusBeforeLocking { get; set; }
    
    /// <summary>
    /// 异常原因（如果任务状态为Incomplete）
    /// </summary>
    public string? ExceptionReason { get; set; }
}
