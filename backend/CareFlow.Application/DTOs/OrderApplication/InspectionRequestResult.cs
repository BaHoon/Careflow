namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 检查站系统请求结果
/// </summary>
public class InspectionRequestResult
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
    /// 检查站接受的医嘱ID列表
    /// </summary>
    public List<long> AcceptedOrderIds { get; set; } = new();
    
    /// <summary>
    /// 预约信息
    /// Key: 医嘱ID, Value: 预约号
    /// </summary>
    public Dictionary<long, string> AppointmentNumbers { get; set; } = new();
}
