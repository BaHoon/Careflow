using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 医嘱摘要DTO
/// 用于在医嘱列表中展示基础信息，不含详细内容
/// </summary>
public class OrderSummaryDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 医嘱类型（MedicationOrder, SurgicalOrder, InspectionOrder, OperationOrder）
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 医嘱状态
    /// </summary>
    public OrderStatus Status { get; set; }
    
    /// <summary>
    /// 是否长期医嘱
    /// </summary>
    public bool IsLongTerm { get; set; }
    
    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 计划结束时间（UTC）
    /// </summary>
    public DateTime PlantEndTime { get; set; }
    
    /// <summary>
    /// 医嘱摘要描述（根据类型生成，如"盐酸氨溴索注射液 15mg"）
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// 开单医生ID
    /// </summary>
    public string DoctorId { get; set; } = null!;
    
    /// <summary>
    /// 开单医生姓名
    /// </summary>
    public string DoctorName { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联任务数量
    /// </summary>
    public int TaskCount { get; set; }
    
    /// <summary>
    /// 已完成任务数量（包含 Completed 和 Incomplete 状态）
    /// </summary>
    public int CompletedTaskCount { get; set; }
    
    /// <summary>
    /// 停嘱时间（如果状态为PendingStop或Stopped）
    /// </summary>
    public DateTime? StopOrderTime { get; set; }
    
    /// <summary>
    /// 停嘱原因
    /// </summary>
    public string? StopReason { get; set; }
    
    /// <summary>
    /// 报告ID（仅检查医嘱）
    /// </summary>
    public string? ReportId { get; set; }
    
    /// <summary>
    /// 附件URL（报告PDF文件路径，仅检查医嘱）
    /// </summary>
    public string? AttachmentUrl { get; set; }
}
