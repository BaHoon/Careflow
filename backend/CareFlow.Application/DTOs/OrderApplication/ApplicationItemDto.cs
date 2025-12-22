namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 申请项DTO（用于前端展示）
/// </summary>
public class ApplicationItemDto
{
    /// <summary>
    /// 申请类型：Medication | Inspection
    /// </summary>
    public string ApplicationType { get; set; } = null!;
    
    /// <summary>
    /// 关联ID（取药任务ID或检查医嘱ID）
    /// </summary>
    public long RelatedId { get; set; }
    
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    // === 患者信息 ===
    public string PatientId { get; set; } = null!;
    public string PatientName { get; set; } = null!;
    public string BedId { get; set; } = null!;
    
    /// <summary>
    /// 申请状态：Applying | Applied | AppliedConfirmed
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// 状态描述（中文）
    /// </summary>
    public string StatusText { get; set; } = null!;
    
    /// <summary>
    /// 计划开始时间
    /// </summary>
    public DateTime PlannedStartTime { get; set; }
    
    /// <summary>
    /// 申请内容描述
    /// </summary>
    public string ContentDescription { get; set; } = null!;
    
    /// <summary>
    /// 药品明细（药品申请专用）
    /// </summary>
    public List<MedicationItemDetail>? Medications { get; set; }
    
    /// <summary>
    /// 检查项目（检查申请专用）
    /// </summary>
    public InspectionDetail? InspectionInfo { get; set; }
    
    /// <summary>
    /// 是否加急
    /// </summary>
    public bool IsUrgent { get; set; }
    
    /// <summary>
    /// 申请备注
    /// </summary>
    public string? Remarks { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 申请时间（如果已申请）
    /// </summary>
    public DateTime? AppliedAt { get; set; }
    
    /// <summary>
    /// 申请护士ID
    /// </summary>
    public string? AppliedBy { get; set; }
    
    /// <summary>
    /// 确认时间（如果已确认）
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
}

/// <summary>
/// 药品明细
/// </summary>
public class MedicationItemDetail
{
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = null!;
    public string Specification { get; set; } = null!;
    public string Dosage { get; set; } = null!;
}

/// <summary>
/// 检查详情
/// </summary>
public class InspectionDetail
{
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string Source { get; set; } = null!; // RIS | LIS
    public string? Precautions { get; set; }
    public DateTime? AppointmentTime { get; set; }
    public string? AppointmentPlace { get; set; }
}
