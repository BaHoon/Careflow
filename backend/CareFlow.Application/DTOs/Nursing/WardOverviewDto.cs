using CareFlow.Core.Enums;
namespace CareFlow.Application.DTOs.Nursing;

/// <summary>
/// 床位概览数据（用于护士工作台）
/// </summary>
public class BedOverviewDto
{
    public string BedId { get; set; } = string.Empty;
    public string BedStatus { get; set; } = string.Empty; // 空闲/占用
    public string WardId { get; set; } = string.Empty;
    public PatientSummaryDto? Patient { get; set; } // 患者信息（空床时为null）
    public BedStatusFlagsDto StatusFlags { get; set; } = new();
}

/// <summary>
/// 患者摘要信息
/// </summary>
public class PatientSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public int NursingGrade { get; set; } // 0=特级, 1=一级, 2=二级, 3=三级
    public string BedId { get; set; } = string.Empty;
}

/// <summary>
/// 床位状态标识
/// </summary>
public class BedStatusFlagsDto
{
    /// <summary>今日有待执行手术</summary>
    public bool HasSurgeryToday { get; set; }
    
    /// <summary>有体征异常（体温过高/过低等）</summary>
    public bool HasAbnormalVitalSign { get; set; }
    
    /// <summary>有新开医嘱（未生成执行任务）</summary>
    public bool HasNewOrder { get; set; }
    
    /// <summary>有待执行任务</summary>
    public bool HasPendingTask { get; set; }
    
    /// <summary>有超时任务</summary>
    public bool HasOverdueTask { get; set; }
}

/// <summary>
/// 病区概览响应
/// </summary>
public class WardOverviewResponseDto
{
    public string WardId { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public List<BedOverviewDto> Beds { get; set; } = new();
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public int AvailableBeds { get; set; }
}

/// <summary>
/// 护士任务查询请求
/// </summary>
public class NurseTaskQueryDto
{
    public string NurseId { get; set; } = string.Empty;
    public DateTime? Date { get; set; } // 查询日期，默认今天
    public string? Status { get; set; } // 任务状态筛选
    public string? Category { get; set; } // 任务类别筛选
}

/// <summary>
/// 护士任务响应（包含患者信息）
/// </summary>
public class NurseTaskDto
{
    public long Id { get; set; }
    public long MedicalOrderId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string BedId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public ExecutionTaskStatus Status { get; set; } = ExecutionTaskStatus.Pending;
    public string DataPayload { get; set; } = string.Empty;
    public string? ResultPayload { get; set; }
    public bool IsOverdue { get; set; } // 计算字段：是否超时
    public bool IsDueSoon { get; set; } // 计算字段：是否临期（30分钟内）
}
