namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 医生处理异常任务响应DTO
/// </summary>
public class HandleAbnormalResponseDto
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 处理后的医嘱状态
    /// </summary>
    public Core.Enums.OrderStatus NewOrderStatus { get; set; }
    
    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误信息列表（如果失败）
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
    
    /// <summary>
    /// 未完成任务数量
    /// </summary>
    public int PendingTaskCount { get; set; }
    
    /// <summary>
    /// 异常任务数量
    /// </summary>
    public int AbnormalTaskCount { get; set; }
}
