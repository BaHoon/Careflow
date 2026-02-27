namespace CareFlow.Application.DTOs.OperationOrder;

/// <summary>
/// 创建操作医嘱请求DTO（兼容旧接口，支持新时间策略）
/// </summary>
public class CreateOperationOrderRequestDto
{
    // === 基础信息 ===
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public bool IsLongTerm { get; set; }
    public DateTime PlantEndTime { get; set; } // 医嘱结束时间
    public string? Remarks { get; set; } // 备注

    // === 操作信息 ===
    public string OpId { get; set; } = null!; // 操作代码，如 "OP001"
    public string? OperationName { get; set; } // 操作名称（可选，系统可根据OpId自动填充）
    public string? OperationSite { get; set; } // 操作部位
    public bool Normal { get; set; } = true; // 正常/异常标识
    
    // === 时间策略（新设计，优先使用） ===
    public string? TimingStrategy { get; set; } // IMMEDIATE/SPECIFIC/CYCLIC/SLOTS
    public DateTime? StartTime { get; set; }
    public decimal? IntervalHours { get; set; }
    public int SmartSlotsMask { get; set; }
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

