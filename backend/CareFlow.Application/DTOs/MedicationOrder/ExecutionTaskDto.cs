namespace CareFlow.Application.DTOs.MedicationOrder;

public class ExecutionTaskDto
{
    public long Id { get; set; }
    public long MedicalOrderId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public string? ExecutorStaffId { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsRolledBack { get; set; }
    public string DataPayload { get; set; } = string.Empty;
    public string ExceptionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
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