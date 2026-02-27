using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 查询医嘱请求DTO
/// 用于医生端查询患者的医嘱列表，支持多条件筛选
/// </summary>
public class QueryOrdersRequestDto
{
    /// <summary>
    /// 患者ID（必填）
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 医嘱状态筛选（可多选）
    /// 如果为空或null，则不按状态筛选
    /// </summary>
    public List<OrderStatus>? Statuses { get; set; }
    
    /// <summary>
    /// 医嘱类型筛选（可多选）
    /// 例如：["MedicationOrder", "SurgicalOrder", "InspectionOrder", "OperationOrder"]
    /// 如果为空或null，则不按类型筛选
    /// </summary>
    public List<string>? OrderTypes { get; set; }
    
    /// <summary>
    /// 创建时间范围 - 开始时间（UTC）
    /// 如果为null，则不限制开始时间
    /// </summary>
    public DateTime? CreateTimeFrom { get; set; }
    
    /// <summary>
    /// 创建时间范围 - 结束时间（UTC）
    /// 如果为null，则不限制结束时间
    /// </summary>
    public DateTime? CreateTimeTo { get; set; }
    
    /// <summary>
    /// 排序字段（默认按创建时间倒序）
    /// 可选值：CreateTime, Status, OrderType
    /// </summary>
    public string? SortBy { get; set; } = "CreateTime";
    
    /// <summary>
    /// 排序方向（默认降序）
    /// true = 降序(Descending), false = 升序(Ascending)
    /// </summary>
    public bool SortDescending { get; set; } = true;
}
