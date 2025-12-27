using CareFlow.Application.DTOs.MedicationOrders;

namespace CareFlow.Application.DTOs.DischargeOrders;

/// <summary>
/// 批量创建出院医嘱请求DTO
/// </summary>
public class BatchCreateDischargeOrderRequestDto
{
    /// <summary>
    /// 患者ID（必填）
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 医生ID（必填）
    /// </summary>
    public string DoctorId { get; set; } = null!;
    
    /// <summary>
    /// 出院医嘱列表
    /// </summary>
    public List<DischargeOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// 单个出院医嘱DTO
/// </summary>
public class DischargeOrderDto
{
    /// <summary>
    /// 出院类型（1=治愈出院, 2=好转出院, 3=转院, 4=自动出院, 5=死亡, 99=其他）
    /// </summary>
    public int DischargeType { get; set; }
    
    /// <summary>
    /// 出院时间（前端传入北京时间，后端会转换为UTC）
    /// </summary>
    public DateTime DischargeTime { get; set; }
    
    /// <summary>
    /// 出院诊断（必填）
    /// </summary>
    public string DischargeDiagnosis { get; set; } = null!;
    
    /// <summary>
    /// 出院医嘱（休息、饮食、活动等建议）
    /// </summary>
    public string DischargeInstructions { get; set; } = string.Empty;
    
    /// <summary>
    /// 出院带药说明（服药注意事项、复查要求等）
    /// </summary>
    public string MedicationInstructions { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否需要随访
    /// </summary>
    public bool RequiresFollowUp { get; set; }
    
    /// <summary>
    /// 随访时间（如果 RequiresFollowUp=true 则必填）
    /// </summary>
    public DateTime? FollowUpDate { get; set; }
    
    /// <summary>
    /// 出院带药清单（可选，可以为空）
    /// </summary>
    public List<MedicationOrderItemDto>? Items { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
