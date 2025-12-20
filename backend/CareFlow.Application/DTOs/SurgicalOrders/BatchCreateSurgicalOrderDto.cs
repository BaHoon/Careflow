namespace CareFlow.Application.DTOs.SurgicalOrders;

/// <summary>
/// 批量创建手术医嘱请求DTO
/// </summary>
public class BatchCreateSurgicalOrderRequestDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public List<SurgicalOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个手术医嘱DTO
/// </summary>
public class SurgicalOrderDto
{
    /// <summary>
    /// 手术名称
    /// </summary>
    public string SurgeryName { get; set; } = null!;
    
    /// <summary>
    /// 手术类型（Elective、Emergency）
    /// </summary>
    public string SurgeryType { get; set; } = null!;
    
    /// <summary>
    /// 麻醉方式（General、Local、Epidural等）
    /// </summary>
    public string AnesthesiaMethod { get; set; } = null!;
    
    /// <summary>
    /// 主刀医生ID
    /// </summary>
    public string SurgeonId { get; set; } = null!;
    
    /// <summary>
    /// 助手医生ID列表
    /// </summary>
    public List<string> AssistantIds { get; set; } = new();
    
    /// <summary>
    /// 计划手术时间
    /// </summary>
    public DateTime ScheduledTime { get; set; }
    
    /// <summary>
    /// 预计时长（分钟）
    /// </summary>
    public int? EstimatedDuration { get; set; }
    
    /// <summary>
    /// 手术室
    /// </summary>
    public string? OperatingRoom { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
