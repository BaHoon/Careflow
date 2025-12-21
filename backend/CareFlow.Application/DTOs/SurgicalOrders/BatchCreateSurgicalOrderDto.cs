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
    /// 麻醉方式
    /// </summary>
    public string AnesthesiaType { get; set; } = null!;
    
    /// <summary>
    /// 切口部位
    /// </summary>
    public string IncisionSite { get; set; } = null!;
    
    /// <summary>
    /// 主刀医生ID
    /// </summary>
    public string SurgeonId { get; set; } = null!;
    
    /// <summary>
    /// 计划手术时间
    /// </summary>
    public DateTime ScheduleTime { get; set; }
    
    /// <summary>
    /// 术前宣讲事项（JSON数组）
    /// </summary>
    public List<string>? RequiredTalk { get; set; }
    
    /// <summary>
    /// 术前操作事项（JSON数组）
    /// </summary>
    public List<string>? RequiredOperation { get; set; }
    
    /// <summary>
    /// 手术药品列表
    /// </summary>
    public List<SurgicalDrugItemDto>? Items { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 手术药品项DTO
/// </summary>
public class SurgicalDrugItemDto
{
    /// <summary>
    /// 药品ID
    /// </summary>
    public string DrugId { get; set; } = null!;
    
    /// <summary>
    /// 剂量
    /// </summary>
    public string Dosage { get; set; } = null!;
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; set; }
}
