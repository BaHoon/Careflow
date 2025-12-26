namespace CareFlow.Application.DTOs.OperationOrders;

/// <summary>
/// 生成操作医嘱任务请求DTO
/// </summary>
public class GenerateTasksRequestDto
{
    public long OperationOrderId { get; set; }
}

/// <summary>
/// 生成操作医嘱任务响应DTO
/// </summary>
public class GenerateTasksResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<ExecutionTaskDto> Tasks { get; set; } = new();
    public int TaskCount { get; set; }
}

/// <summary>
/// 执行任务DTO
/// </summary>
public class ExecutionTaskDto
{
    public long Id { get; set; }
    public long MedicalOrderId { get; set; }
    public string PatientId { get; set; } = null!;
    public int Category { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public string? ExecutorStaffId { get; set; }
    public string? CompleterNurseId { get; set; }
    public int Status { get; set; }
    public bool IsRolledBack { get; set; }
    public string DataPayload { get; set; } = string.Empty;
    public string? ResultPayload { get; set; }
    public string ExceptionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

