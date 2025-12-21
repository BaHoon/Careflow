namespace CareFlow.Application.DTOs.MedicationOrders;
using CareFlow.Core.Enums;

public class ExecutionTaskDto
{
    public long Id { get; set; }
    public long MedicalOrderId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public TaskCategory Category { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public string? ExecutorStaffId { get; set; }
    public string? CompleterNurseId { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsRolledBack { get; set; }
    public string DataPayload { get; set; } = string.Empty;
    public string? ResultPayload { get; set; }
    public string ExceptionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // --- 延迟状态字段（新增）---
    
    /// <summary>
    /// 实际延迟时长（分钟）
    /// </summary>
    public int DelayMinutes { get; set; }
    
    /// <summary>
    /// 允许延迟时长（分钟）
    /// </summary>
    public int AllowedDelayMinutes { get; set; }
    
    /// <summary>
    /// 超出允许范围的时长（分钟）
    /// </summary>
    public int ExcessDelayMinutes { get; set; }
    
    /// <summary>
    /// 严重程度级别：Normal | Warning | Severe
    /// </summary>
    public string SeverityLevel { get; set; } = "Normal";
}

public class GenerateTasksRequestDto
{
    public long MedicationOrderId { get; set; }
}

public class GenerateTasksResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ExecutionTaskDto> Tasks { get; set; } = new();
    public int TaskCount { get; set; }
}