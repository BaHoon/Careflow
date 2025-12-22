namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 单条待签收医嘱DTO
/// </summary>
public class PendingOrderDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱类型（MedicationOrder/InspectionOrder/SurgicalOrder/OperationOrder）
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 是否长期医嘱
    /// </summary>
    public bool IsLongTerm { get; set; }
    
    /// <summary>
    /// 医嘱内容摘要（用于前端显示）
    /// </summary>
    public string DisplayText { get; set; } = null!;
    
    /// <summary>
    /// 开立时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 医生姓名
    /// </summary>
    public string DoctorName { get; set; } = null!;
    
    /// <summary>
    /// 医生工号
    /// </summary>
    public string DoctorId { get; set; } = null!;
    
    // === 药品医嘱特有字段 ===
    
    /// <summary>
    /// 药品明细列表（仅药品医嘱和手术医嘱有）
    /// </summary>
    public List<OrderItemDto>? Items { get; set; }
    
    /// <summary>
    /// 给药途径（仅药品医嘱）
    /// </summary>
    public string? UsageRoute { get; set; }
    
    /// <summary>
    /// 时间策略（仅药品医嘱）
    /// </summary>
    public string? TimingStrategy { get; set; }
    
    /// <summary>
    /// 开始时间（仅药品医嘱）
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 计划结束时间
    /// </summary>
    public DateTime? PlantEndTime { get; set; }
    
    // === 停止医嘱特有字段 ===
    
    /// <summary>
    /// 停止时间（仅停止医嘱）
    /// </summary>
    public DateTime? StopTime { get; set; }
    
    /// <summary>
    /// 停止原因（仅停止医嘱）
    /// </summary>
    public string? StopReason { get; set; }
    
    /// <summary>
    /// 停止到哪个任务（仅停止医嘱）
    /// TODO: 需要医生停止医嘱时设置此字段
    /// </summary>
    public long? StopUntilTaskId { get; set; }
    
    // === 检查医嘱特有字段 ===
    
    /// <summary>
    /// 检查项目代码（仅检查医嘱）
    /// </summary>
    public string? ItemCode { get; set; }
    
    /// <summary>
    /// 检查地点（仅检查医嘱）
    /// </summary>
    public string? Location { get; set; }
    
    // === 手术医嘱特有字段 ===
    
    /// <summary>
    /// 手术名称（仅手术医嘱）
    /// </summary>
    public string? SurgeryName { get; set; }
    
    /// <summary>
    /// 排期时间（仅手术医嘱）
    /// </summary>
    public DateTime? ScheduleTime { get; set; }
    
    /// <summary>
    /// 麻醉方式（仅手术医嘱）
    /// </summary>
    public string? AnesthesiaType { get; set; }
    
    // === 操作医嘱特有字段 ===
    
    /// <summary>
    /// 操作代码（仅操作医嘱）
    /// </summary>
    public string? OpId { get; set; }
    
    /// <summary>
    /// 医嘱备注
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 医嘱药品明细DTO
/// </summary>
public class OrderItemDto
{
    /// <summary>
    /// 药品ID
    /// </summary>
    public string DrugId { get; set; } = null!;
    
    /// <summary>
    /// 药品名称
    /// </summary>
    public string DrugName { get; set; } = null!;
    
    /// <summary>
    /// 规格
    /// </summary>
    public string Specification { get; set; } = null!;
    
    /// <summary>
    /// 剂量
    /// </summary>
    public string Dosage { get; set; } = null!;
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; set; }
}
