using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 医嘱详情DTO
/// 包含完整的医嘱信息和关联任务列表
/// 根据OrderType不同，某些字段可能为null
/// </summary>
public class OrderDetailDto
{
    // ==================== 基础字段（所有医嘱类型共有） ====================
    
    public long Id { get; set; }
    public string OrderType { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public bool IsLongTerm { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime PlantEndTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Remarks { get; set; }
    
    // 人员信息
    public string PatientId { get; set; } = null!;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = null!;
    public string DoctorName { get; set; } = string.Empty;
    public string? NurseId { get; set; }
    public string? NurseName { get; set; }
    
    // 审计字段 - 签收
    public string? SignedByNurseId { get; set; }
    public string? SignedByNurseName { get; set; }
    public DateTime? SignedAt { get; set; }
    
    // 审计字段 - 停嘱
    public string? StopReason { get; set; }
    public DateTime? StopOrderTime { get; set; }
    public string? StopDoctorId { get; set; }
    public string? StopDoctorName { get; set; }
    public DateTime? StopConfirmedAt { get; set; }
    public string? StopConfirmedByNurseId { get; set; }
    public string? StopConfirmedByNurseName { get; set; }
    
    // ==================== 药品医嘱特有字段 ====================
    
    /// <summary>
    /// 用药途径（仅MedicationOrder有效）
    /// </summary>
    public UsageRoute? UsageRoute { get; set; }
    
    /// <summary>
    /// 时间策略（仅MedicationOrder有效）
    /// </summary>
    public string? TimingStrategy { get; set; }
    
    /// <summary>
    /// 开始时间（仅MedicationOrder有效）
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 间隔小时（仅MedicationOrder的Cyclic策略有效）
    /// </summary>
    public decimal? IntervalHours { get; set; }
    
    /// <summary>
    /// 间隔天数（仅MedicationOrder有效）
    /// </summary>
    public int? IntervalDays { get; set; }
    
    /// <summary>
    /// 时段掩码（仅MedicationOrder的Slots策略有效）
    /// </summary>
    public int? SmartSlotsMask { get; set; }
    
    /// <summary>
    /// 药品项目列表（仅MedicationOrder有效）
    /// </summary>
    public List<MedicationItemDto>? MedicationItems { get; set; }
    
    // ==================== 手术医嘱特有字段 ====================
    
    /// <summary>
    /// 手术名称（仅SurgicalOrder有效）
    /// </summary>
    public string? SurgeryName { get; set; }
    
    /// <summary>
    /// 手术时间（仅SurgicalOrder有效）
    /// </summary>
    public DateTime? ScheduleTime { get; set; }
    
    /// <summary>
    /// 麻醉方式（仅SurgicalOrder有效）
    /// </summary>
    public string? AnesthesiaType { get; set; }
    
    /// <summary>
    /// 切口部位（仅SurgicalOrder有效）
    /// </summary>
    public string? IncisionSite { get; set; }
    
    /// <summary>
    /// 主刀医生ID（仅SurgicalOrder有效）
    /// </summary>
    public string? SurgeonId { get; set; }
    
    /// <summary>
    /// 主刀医生姓名（仅SurgicalOrder有效）
    /// </summary>
    public string? SurgeonName { get; set; }
    
    /// <summary>
    /// 术前宣讲事项（仅SurgicalOrder有效）
    /// </summary>
    public List<string>? RequiredTalk { get; set; }
    
    /// <summary>
    /// 术前操作事项（仅SurgicalOrder有效）
    /// </summary>
    public List<string>? RequiredOperation { get; set; }
    
    /// <summary>
    /// 手术药品项目（仅SurgicalOrder有效）
    /// </summary>
    public List<MedicationItemDto>? SurgicalItems { get; set; }
    
    // ==================== 检查医嘱特有字段 ====================
    
    /// <summary>
    /// 检查项目代码（仅InspectionOrder有效）
    /// </summary>
    public string? ItemCode { get; set; }
    
    /// <summary>
    /// 检查项目名称（仅InspectionOrder有效）
    /// </summary>
    public string? ItemName { get; set; }
    
    // ==================== 操作医嘱特有字段 ====================
    
    /// <summary>
    /// 操作代码（仅OperationOrder有效）
    /// </summary>
    public string? OperationCode { get; set; }
    
    /// <summary>
    /// 操作名称（仅OperationOrder有效）
    /// </summary>
    public string? OperationName { get; set; }
    
    /// <summary>
    /// 操作部位（仅OperationOrder有效）
    /// </summary>
    public string? TargetSite { get; set; }
    
    // ==================== 关联任务列表 ====================
    
    /// <summary>
    /// 关联的执行任务列表
    /// </summary>
    public List<TaskSummaryDto> Tasks { get; set; } = new();
}

/// <summary>
/// 药品项目DTO
/// 用于展示药品医嘱或手术医嘱中的药品列表
/// </summary>
public class MedicationItemDto
{
    public long Id { get; set; }
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = null!;
    public string? Note { get; set; }
}
