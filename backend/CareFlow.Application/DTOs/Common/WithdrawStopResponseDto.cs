using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 医生撤回停嘱申请响应DTO
/// </summary>
public class WithdrawStopResponseDto
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
    /// 医嘱回滚后的状态（Accepted 或 InProgress）
    /// </summary>
    public OrderStatus RestoredOrderStatus { get; set; }
    
    /// <summary>
    /// 被恢复的任务ID列表
    /// </summary>
    public List<long> RestoredTaskIds { get; set; } = new();
    
    /// <summary>
    /// 任务恢复详情（任务ID -> 恢复后的状态）
    /// </summary>
    public Dictionary<long, string> TaskRestorationDetails { get; set; } = new();
    
    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
