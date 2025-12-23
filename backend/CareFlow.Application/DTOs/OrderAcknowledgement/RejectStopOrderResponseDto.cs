namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 护士拒绝停嘱响应DTO
/// </summary>
public class RejectStopOrderResponseDto
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 操作结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 成功拒绝的医嘱ID列表
    /// </summary>
    public List<long> RejectedOrderIds { get; set; } = new();
    
    /// <summary>
    /// 恢复的任务ID列表
    /// 这些任务从 OrderStopping 状态恢复为锁定前的原始状态
    /// </summary>
    public List<long> RestoredTaskIds { get; set; } = new();
    
    /// <summary>
    /// 任务状态恢复详情（用于审计和日志）
    /// Key: TaskId, Value: 恢复后的状态
    /// </summary>
    public Dictionary<long, string> TaskRestorationDetails { get; set; } = new();
    
    /// <summary>
    /// 错误信息列表（如果有部分失败）
    /// </summary>
    public List<string>? Errors { get; set; }
}
