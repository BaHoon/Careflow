namespace CareFlow.Application.DTOs.OperationOrders;

/// <summary>
/// 批量创建操作医嘱请求DTO
/// </summary>
public class BatchCreateOperationOrderRequestDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public List<OperationOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个操作医嘱DTO
/// </summary>
public class OperationOrderDto
{
    // === 基础信息 ===
    public bool IsLongTerm { get; set; }
    public DateTime PlantEndTime { get; set; }
    public string? Remarks { get; set; }
    
    // === 操作信息 ===
    public string OpId { get; set; } = null!;
    public string OperationName { get; set; } = null!;
    public string? OperationSite { get; set; }
    public bool Normal { get; set; } = true;
    
    // === 时间策略（新设计，参照药品医嘱） ===
    /// <summary>
    /// 时间策略类型：IMMEDIATE/SPECIFIC/CYCLIC/SLOTS
    /// </summary>
    public string TimingStrategy { get; set; } = null!;
    
    /// <summary>
    /// 开始/执行时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 执行间隔（小时数）- 仅用于 CYCLIC 策略
    /// </summary>
    public decimal? IntervalHours { get; set; }
    
    /// <summary>
    /// 时段位掩码 - 仅用于 SLOTS 策略
    /// </summary>
    public int SmartSlotsMask { get; set; }
    
    /// <summary>
    /// 间隔天数(1=每天, 2=隔天)
    /// </summary>
    public int IntervalDays { get; set; } = 1;
    
    // === 执行要求 ===
    public Dictionary<string, object>? OperationRequirements { get; set; }
    public bool RequiresPreparation { get; set; } = false;
    public List<string>? PreparationItems { get; set; }
    
    // === 任务配置 ===
    public int? ExpectedDurationMinutes { get; set; }
    public bool RequiresResult { get; set; } = false;
    public Dictionary<string, object>? ResultTemplate { get; set; }
}
