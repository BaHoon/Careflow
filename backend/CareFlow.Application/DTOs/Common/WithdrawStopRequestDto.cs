using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 医生撤回停嘱申请请求DTO
/// 用于医生撤回自己下达的停嘱指令（医嘱状态从PendingStop回滚）
/// </summary>
public class WithdrawStopRequestDto
{
    /// <summary>
    /// 医嘱ID（必填）
    /// </summary>
    [Required(ErrorMessage = "医嘱ID不能为空")]
    public long OrderId { get; set; }
    
    /// <summary>
    /// 撤回操作的医生ID（必填）
    /// </summary>
    [Required(ErrorMessage = "医生ID不能为空")]
    public string DoctorId { get; set; } = null!;
    
    /// <summary>
    /// 撤回原因（必填）
    /// </summary>
    [Required(ErrorMessage = "撤回原因不能为空")]
    [StringLength(500, ErrorMessage = "撤回原因不能超过500字")]
    public string WithdrawReason { get; set; } = null!;
}
