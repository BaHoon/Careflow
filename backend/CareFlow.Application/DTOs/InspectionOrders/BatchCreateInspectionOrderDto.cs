namespace CareFlow.Application.DTOs.InspectionOrders;

/// <summary>
/// 批量创建检查医嘱请求DTO
/// </summary>
public class BatchCreateInspectionOrderRequestDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public List<InspectionOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个检查医嘱DTO
/// </summary>
public class InspectionOrderDto
{
    /// <summary>
    /// 检查类型（CT、MRI、X-Ray、Ultrasound等）
    /// </summary>
    public string InspectionType { get; set; } = null!;
    
    /// <summary>
    /// 检查部位（Head、Chest、Abdomen等）
    /// </summary>
    public string TargetOrgan { get; set; } = null!;
    
    /// <summary>
    /// 紧急程度（urgent、normal、routine）
    /// </summary>
    public string Urgency { get; set; } = "normal";
    
    /// <summary>
    /// 是否需要造影剂
    /// </summary>
    public bool Contrast { get; set; }
    
    /// <summary>
    /// 预约时间
    /// </summary>
    public DateTime ScheduledTime { get; set; }
    
    /// <summary>
    /// 临床资料（症状、病史等）
    /// </summary>
    public string? ClinicalInfo { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
