using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 停止医嘱响应DTO
/// 返回停嘱操作的结果和被锁定的任务列表
/// </summary>
public class StopOrderResponseDto
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱更新后的状态（应为PendingStop）
    /// </summary>
    public OrderStatus OrderStatus { get; set; }
    
    /// <summary>
    /// 停嘱时间
    /// </summary>
    public DateTime StopOrderTime { get; set; }
    
    /// <summary>
    /// 被锁定的任务ID列表
    /// </summary>
    public List<long> LockedTaskIds { get; set; } = new();
    
    /// <summary>
    /// 被锁定的任务详情
    /// </summary>
    public List<LockedTaskDto> LockedTasks { get; set; } = new();
    
    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// 被锁定的任务DTO
/// </summary>
public class LockedTaskDto
{
    public long TaskId { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public ExecutionTaskStatus StatusBeforeLocking { get; set; }
    public ExecutionTaskStatus CurrentStatus { get; set; }
}
