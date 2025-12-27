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
    
    /// <summary>
    /// 任务来源：NursingTask（护理任务）或 ExecutionTask（医嘱执行任务）
    /// </summary>
    public string TaskSource { get; set; } = string.Empty;
    
    public long MedicalOrderId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string BedId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 负责护士ID
    /// </summary>
    public string? AssignedNurseId { get; set; }
    
    /// <summary>
    /// 负责护士姓名
    /// </summary>
    public string? AssignedNurseName { get; set; }
    
    /// <summary>
    /// 实际执行护士ID（可能与责任护士不同）
    /// </summary>
    public string? ExecutorNurseId { get; set; }
    
    /// <summary>
    /// 实际执行护士姓名
    /// </summary>
    public string? ExecutorNurseName { get; set; }
    
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public ExecutionTaskStatus Status { get; set; } = ExecutionTaskStatus.Pending;
    public string DataPayload { get; set; } = string.Empty;
    public string? ResultPayload { get; set; }
    
    // --- 延迟状态字段（新增）---
    
    /// <summary>
    /// 实际延迟时长（分钟）
    /// 从计划时间到现在的延迟，可能为负数（表示还没到时间）
    /// </summary>
    public int DelayMinutes { get; set; }
    
    /// <summary>
    /// 允许延迟时长（分钟）
    /// 不同类型任务的正常范围不同，如常规护理90分钟，复测30分钟
    /// </summary>
    public int AllowedDelayMinutes { get; set; }
    
    /// <summary>
    /// 超出允许范围的时长（分钟）
    /// 正数表示已超出正常范围，0或负数表示在正常范围内
    /// </summary>
    public int ExcessDelayMinutes { get; set; }
    
    /// <summary>
    /// 严重程度级别
    /// Normal: 正常范围内
    /// Warning: 超出正常范围0-30分钟
    /// Severe: 超出正常范围30分钟以上
    /// </summary>
    public string SeverityLevel { get; set; } = "Normal";
    
    /// <summary>
    /// 是否超时（向后兼容）
    /// </summary>
    public bool IsOverdue { get; set; }
    
    /// <summary>
    /// 是否临期（30分钟内，向后兼容）
    /// </summary>
    public bool IsDueSoon { get; set; }
    
    /// <summary>
    /// 医嘱类型名称（ExecutionTask专用）
    /// 如：药品医嘱、手术医嘱、检查医嘱等
    /// </summary>
    public string? OrderTypeName { get; set; }
    
    /// <summary>
    /// 任务标题（ExecutionTask专用）
    /// 从DataPayload中提取的任务描述，如"口服阿司匹林 100mg"
    /// </summary>
    public string? TaskTitle { get; set; }
    
    /// <summary>
    /// 体征数据（仅已完成的护理任务有此数据）
    /// </summary>
    public object? VitalSigns { get; set; }
}
