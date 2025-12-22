namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 申请操作响应DTO
/// </summary>
public class ApplicationResponseDto
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// 成功处理的ID列表
    /// </summary>
    public List<long> ProcessedIds { get; set; } = new();
    
    /// <summary>
    /// 错误信息列表（如果有）
    /// </summary>
    public List<string>? Errors { get; set; }
    
    /// <summary>
    /// 预计完成时间（药房申请专用）
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }
    
    /// <summary>
    /// 预约信息（检查申请专用）
    /// Key: OrderId, Value: AppointmentNumber
    /// </summary>
    public Dictionary<long, string>? AppointmentInfo { get; set; }
}
