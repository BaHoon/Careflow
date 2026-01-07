using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 医生处理异常任务请求DTO
/// 用于医生处理异常态医嘱，确认已知晓异常情况
/// </summary>
public class HandleAbnormalRequestDto
{
    /// <summary>
    /// 医嘱ID（必填）
    /// </summary>
    [Required(ErrorMessage = "医嘱ID不能为空")]
    public long OrderId { get; set; }
    
    /// <summary>
    /// 处理操作的医生ID（必填）
    /// </summary>
    [Required(ErrorMessage = "医生ID不能为空")]
    public string DoctorId { get; set; } = null!;
    
    /// <summary>
    /// 处理说明（可选）
    /// 医生对异常的处理意见或备注
    /// </summary>
    [StringLength(500, ErrorMessage = "处理说明不能超过500字")]
    public string? HandleNote { get; set; }
}
