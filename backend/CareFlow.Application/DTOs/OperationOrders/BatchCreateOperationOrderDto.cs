namespace CareFlow.Application.DTOs.OperationOrders;

/// <summary>
/// 批量创建操作医嘱请求DTO
/// </summary>
public class BatchCreateOperationOrderRequestDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public List<OperationOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个操作医嘱DTO（预留，待实现具体字段）
/// </summary>
public class OperationOrderDto
{
    /// <summary>
    /// 操作代码/ID
    /// </summary>
    public string OperationCode { get; set; } = null!;
    
    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName { get; set; } = null!;
    
    /// <summary>
    /// 操作部位
    /// </summary>
    public string? TargetSite { get; set; }
    
    /// <summary>
    /// 预约时间
    /// </summary>
    public DateTime? ScheduledTime { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
