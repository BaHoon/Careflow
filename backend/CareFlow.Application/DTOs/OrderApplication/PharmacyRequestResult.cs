namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 药房系统请求结果
/// </summary>
public class PharmacyRequestResult
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
    /// 药房接受的任务ID列表
    /// </summary>
    public List<long> AcceptedTaskIds { get; set; } = new();
    
    /// <summary>
    /// 预计完成时间
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }
}
