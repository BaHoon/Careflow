namespace CareFlow.Application.DTOs.MedicationOrders;

/// <summary>
/// 批量创建药物医嘱请求DTO
/// </summary>
public class BatchCreateMedicationOrderRequestDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public List<MedicationOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个药物医嘱DTO
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

// 响应DTO已移至 Common 命名空间统一管理
