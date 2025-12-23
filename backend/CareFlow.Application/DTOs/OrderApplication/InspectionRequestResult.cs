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
    
    /// <summary>
    /// 预约详细信息（用于生成任务）
    /// Key: 医嘱ID, Value: 预约详情
    /// </summary>
    public Dictionary<long, AppointmentDetail> AppointmentDetails { get; set; } = new();
}

/// <summary>
/// 预约详细信息
/// </summary>
public class AppointmentDetail
{
    /// <summary>
    /// 预约号
    /// </summary>
    public string AppointmentNumber { get; set; } = null!;
    
    /// <summary>
    /// 预约时间
    /// </summary>
    public DateTime AppointmentTime { get; set; }
    
    /// <summary>
    /// 预约地点
    /// </summary>
    public string AppointmentPlace { get; set; } = null!;
    
    /// <summary>
    /// 注意事项（可选，如空腹、憋尿等）
    /// </summary>
    public string? Precautions { get; set; }
}
