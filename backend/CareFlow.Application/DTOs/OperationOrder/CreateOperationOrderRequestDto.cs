namespace CareFlow.Application.DTOs.OperationOrder;

/// <summary>
/// 创建操作医嘱请求DTO（独立入口，不修改 MedicalOrderController）
/// </summary>
public class CreateOperationOrderRequestDto
{
    // === 基础信息 ===
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public bool IsLongTerm { get; set; }
    public DateTime PlantEndTime { get; set; } // 医嘱结束时间
    public string? Remarks { get; set; } // 备注

    // === 操作医嘱特有字段 ===
    public string OpId { get; set; } = null!; // 操作代码，如 "OP001"
    public bool Normal { get; set; } = true; // 正常/异常标识
    public string FrequencyType { get; set; } = null!; // 频次类型："每天"、"持续"、"一次性"
    public string FrequencyValue { get; set; } = null!; // 频次值："3次"、"24小时"、"1次"
}

