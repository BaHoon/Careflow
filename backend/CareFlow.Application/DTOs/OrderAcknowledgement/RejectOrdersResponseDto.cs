namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 退回医嘱响应DTO
/// </summary>
public class RejectOrdersResponseDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// 成功退回的医嘱ID列表
    /// </summary>
    public List<long> RejectedOrderIds { get; set; } = new();
    
    /// <summary>
    /// 错误列表（如果有）
    /// </summary>
    public List<string>? Errors { get; set; }
}
