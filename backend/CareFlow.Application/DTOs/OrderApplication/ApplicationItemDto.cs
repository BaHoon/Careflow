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
    
    /// <summary>
    /// 医嘱类型：Medication | Inspection | Surgical | Operation
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 是否长期医嘱
    /// </summary>
    public bool IsLongTerm { get; set; }
    
    /// <summary>
    /// 医嘱显示文本（主要内容）
    /// </summary>
    public string DisplayText { get; set; } = null!;
    
    /// <summary>
    /// 项目数量（多药品时使用）
    /// </summary>
    public int ItemCount { get; set; }
    
    /// <summary>
    /// 检查来源（仅检查类医嘱）
    /// </summary>
    public string? InspectionSource { get; set; }
    
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
    /// 计划结束时间
    /// </summary>
    public DateTime? PlantEndTime { get; set; }
    
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
    /// 时间策略类型（药品申请专用）：IMMEDIATE | SPECIFIC | CYCLIC | SLOTS
    /// </summary>
    public string? TimingStrategy { get; set; }
    
    /// <summary>
    /// 用法途径（药品申请专用）
    /// </summary>
    public string? UsageRoute { get; set; }
    
    /// <summary>
    /// 执行间隔小时数（仅 CYCLIC 策略使用）
    /// </summary>
    public decimal? IntervalHours { get; set; }
    
    /// <summary>
    /// 间隔天数（仅 SLOTS 策略使用，1=每天，2=隔天）
    /// </summary>
    public int? IntervalDays { get; set; }
    
    /// <summary>
    /// 时段位掩码（仅 SLOTS 策略使用）
    /// </summary>
    public int? SmartSlotsMask { get; set; }
    
    /// <summary>
    /// 手术名称（手术类医嘱的药品申请专用）
    /// </summary>
    public string? SurgeryName { get; set; }
    
    /// <summary>
    /// 手术排期时间（手术类医嘱的药品申请专用）
    /// </summary>
    public DateTime? SurgeryScheduleTime { get; set; }
    
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
