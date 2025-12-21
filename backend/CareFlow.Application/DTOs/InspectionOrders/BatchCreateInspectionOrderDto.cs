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
    /// 检查项目代码（如 CT_HEAD、MRI_CHEST等）
    /// </summary>
    public string ItemCode { get; set; } = null!;
    
    /// <summary>
    /// 预约时间（可选，不需要预约时为null）
    /// </summary>
    public DateTime? AppointmentTime { get; set; }
    
    /// <summary>
    /// 预约地点（可选）
    /// </summary>
    public string? AppointmentPlace { get; set; }
    
    /// <summary>
    /// 注意事项（如空腹、憋尿等）
    /// </summary>
    public string? Precautions { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
