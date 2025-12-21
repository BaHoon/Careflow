namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 患者未签收医嘱统计DTO（用于红点标注）
/// </summary>
public class PatientUnacknowledgedSummaryDto
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; } = null!;
    
    /// <summary>
    /// 床位号
    /// </summary>
    public string BedId { get; set; } = null!;
    
    /// <summary>
    /// 性别
    /// </summary>
    public string Gender { get; set; } = null!;
    
    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }
    
    /// <summary>
    /// 体重
    /// </summary>
    public float Weight { get; set; }
    
    /// <summary>
    /// 护理等级
    /// </summary>
    public int NursingGrade { get; set; }
    
    /// <summary>
    /// 病区ID
    /// </summary>
    public string WardId { get; set; } = null!;
    
    /// <summary>
    /// 病区名称
    /// </summary>
    public string WardName { get; set; } = null!;
    
    /// <summary>
    /// 未签收医嘱数量
    /// </summary>
    public int UnacknowledgedCount { get; set; }
}
