using CareFlow.Application.DTOs.Patient;

namespace CareFlow.Application.DTOs.DischargeOrders;

/// <summary>
/// 出院医嘱开立验证结果DTO
/// </summary>
public class DischargeOrderValidationResult
{
    /// <summary>
    /// 是否可以创建出院医嘱
    /// </summary>
    public bool CanCreateDischargeOrder { get; set; }
    
    /// <summary>
    /// 阻塞创建的医嘱列表（状态不符合要求的医嘱）
    /// </summary>
    public List<BlockedOrderDto> BlockedOrders { get; set; } = new();
    
    /// <summary>
    /// 待签收停止的医嘱列表（影响出院时间的医嘱）
    /// </summary>
    public List<PendingStopOrderDto> PendingStopOrders { get; set; } = new();
    
    /// <summary>
    /// 最早可出院时间（基于PendingStop医嘱的最后任务时间）
    /// </summary>
    public DateTime? EarliestDischargeTime { get; set; }
    
    /// <summary>
    /// 验证消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 阻塞的医嘱DTO
/// </summary>
public class BlockedOrderDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱类型
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 医嘱状态
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusDisplay { get; set; } = null!;
    
    /// <summary>
    /// 医嘱摘要
    /// </summary>
    public string Summary { get; set; } = null!;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 医嘱开始时间（根据医嘱类型可能是CreateTime或其他时间）
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 医嘱结束时间（计划结束时间）
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 检查项目名称（检查医嘱）
    /// </summary>
    public string? ItemName { get; set; }
    
    /// <summary>
    /// 操作名称（操作医嘱）
    /// </summary>
    public string? OperationName { get; set; }
    
    /// <summary>
    /// 手术名称（手术医嘱）
    /// </summary>
    public string? SurgeryName { get; set; }
    
    /// <summary>
    /// 药品项列表（药品医嘱和出院医嘱）
    /// </summary>
    public List<MedicationItemDto>? MedicationOrderItems { get; set; }
}

/// <summary>
/// 待签收停止的医嘱DTO
/// </summary>
public class PendingStopOrderDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱类型
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 医嘱摘要
    /// </summary>
    public string Summary { get; set; } = null!;
    
    /// <summary>
    /// 该医嘱最后一个任务的计划时间
    /// </summary>
    public DateTime? LatestTaskPlannedTime { get; set; }
}

/// <summary>
/// 出院医嘱签收验证结果DTO
/// </summary>
public class DischargeOrderAcknowledgementValidationResult
{
    /// <summary>
    /// 是否可以签收出院医嘱
    /// </summary>
    public bool CanAcknowledge { get; set; }
    
    /// <summary>
    /// 原因说明
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 未签收的停止医嘱ID列表
    /// </summary>
    public List<long> PendingStopOrderIds { get; set; } = new();
    
    /// <summary>
    /// 未签收的停止医嘱详情
    /// </summary>
    public List<PendingStopOrderDetailDto> PendingStopOrderDetails { get; set; } = new();
    
    /// <summary>
    /// 阻塞医嘱列表（未签收、已签收、正在执行的医嘱）
    /// </summary>
    public List<BlockedOrderDto> BlockedOrders { get; set; } = new();
    
    /// <summary>
    /// 提示消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 未签收停止医嘱详情DTO
/// </summary>
public class PendingStopOrderDetailDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱类型
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 医嘱摘要
    /// </summary>
    public string Summary { get; set; } = null!;
    
    /// <summary>
    /// 停嘱时间
    /// </summary>
    public DateTime StopOrderTime { get; set; }
    
    /// <summary>
    /// 停嘱原因
    /// </summary>
    public string StopReason { get; set; } = string.Empty;
}
