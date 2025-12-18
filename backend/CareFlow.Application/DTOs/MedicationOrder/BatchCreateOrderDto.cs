namespace CareFlow.Application.DTOs.MedicationOrder;

/// <summary>
/// 批量创建医嘱请求DTO
/// </summary>
public class BatchCreateOrderRequestDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public List<MedicationOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个医嘱DTO
/// </summary>
public class MedicationOrderDto
{
    public bool IsLongTerm { get; set; }
    public string TimingStrategy { get; set; } = null!; // IMMEDIATE, SPECIFIC, CYCLIC, SLOTS
    public DateTime? StartTime { get; set; }
    public DateTime PlantEndTime { get; set; }
    public decimal? IntervalHours { get; set; }
    public int IntervalDays { get; set; } = 1;
    public int SmartSlotsMask { get; set; }
    public int UsageRoute { get; set; }
    public string? Remarks { get; set; }
    public List<MedicationOrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// 医嘱药品项DTO
/// </summary>
public class MedicationOrderItemDto
{
    public string DrugId { get; set; } = null!;
    public string Dosage { get; set; } = null!;
    public string? Note { get; set; }
}

/// <summary>
/// 批量创建医嘱响应DTO
/// </summary>
public class BatchCreateOrderResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public BatchCreateOrderDataDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}

public class BatchCreateOrderDataDto
{
    public int CreatedCount { get; set; }
    public List<string> OrderIds { get; set; } = new();
    public int TaskCount { get; set; }
}
