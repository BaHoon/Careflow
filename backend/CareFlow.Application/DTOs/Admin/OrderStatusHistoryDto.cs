namespace CareFlow.Application.DTOs.Admin;

/// <summary>
/// 医嘱状态变更历史DTO
/// </summary>
public class OrderStatusHistoryDto
{
    /// <summary>
    /// 历史记录ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long MedicalOrderId { get; set; }
    
    /// <summary>
    /// 医嘱摘要
    /// </summary>
    public string OrderSummary { get; set; } = string.Empty;
    
    /// <summary>
    /// 医嘱类型
    /// </summary>
    public string OrderType { get; set; } = string.Empty;
    
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; } = string.Empty;
    
    /// <summary>
    /// 床位号
    /// </summary>
    public string BedId { get; set; } = string.Empty;
    
    /// <summary>
    /// 变更前状态
    /// </summary>
    public int FromStatus { get; set; }
    
    /// <summary>
    /// 变更前状态名称
    /// </summary>
    public string FromStatusName { get; set; } = string.Empty;
    
    /// <summary>
    /// 变更后状态
    /// </summary>
    public int ToStatus { get; set; }
    
    /// <summary>
    /// 变更后状态名称
    /// </summary>
    public string ToStatusName { get; set; } = string.Empty;
    
    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangedAt { get; set; }
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    public string ChangedById { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作人姓名
    /// </summary>
    public string ChangedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作人类型（Doctor/Nurse/System）
    /// </summary>
    public string ChangedByType { get; set; } = string.Empty;
    
    /// <summary>
    /// 变更原因
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 查询医嘱状态历史请求DTO
/// </summary>
public class QueryOrderStatusHistoryRequestDto
{
    /// <summary>
    /// 患者ID（可选）
    /// </summary>
    public string? PatientId { get; set; }
    
    /// <summary>
    /// 患者姓名（可选）
    /// </summary>
    public string? PatientName { get; set; }
    
    /// <summary>
    /// 操作人ID（可选）
    /// </summary>
    public string? ChangedById { get; set; }
    
    /// <summary>
    /// 操作人类型（可选：Doctor/Nurse/System）
    /// </summary>
    public string? ChangedByType { get; set; }
    
    /// <summary>
    /// 医嘱类型（可选）
    /// </summary>
    public string? OrderType { get; set; }
    
    /// <summary>
    /// 开始时间（可选）
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间（可选）
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// 医嘱状态历史查询响应DTO
/// </summary>
public class QueryOrderStatusHistoryResponseDto
{
    /// <summary>
    /// 历史记录列表
    /// </summary>
    public List<OrderStatusHistoryDto> Histories { get; set; } = new();
    
    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
