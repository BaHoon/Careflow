using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 停止医嘱请求DTO
/// 医生下达停嘱指令，将医嘱状态改为PendingStop，并锁定指定任务后的所有未完成任务
/// </summary>
public class StopOrderRequestDto
{
    /// <summary>
    /// 医嘱ID（必填）
    /// </summary>
    [Required(ErrorMessage = "医嘱ID不能为空")]
    public long OrderId { get; set; }
    
    /// <summary>
    /// 下达停嘱的医生ID（必填）
    /// </summary>
    [Required(ErrorMessage = "医生ID不能为空")]
    public string DoctorId { get; set; } = null!;
    
    /// <summary>
    /// 停嘱原因（必填）
    /// </summary>
    [Required(ErrorMessage = "停嘱原因不能为空")]
    [StringLength(500, ErrorMessage = "停嘱原因不能超过500字")]
    public string StopReason { get; set; } = null!;
    
    /// <summary>
    /// 停止节点：从哪个任务之后停止（必填）
    /// 该任务本身不会被锁定，只有该任务之后的未完成任务会被锁定
    /// 例如：有5个任务[T1, T2, T3, T4, T5]，如果StopAfterTaskId = T2的ID
    /// 则T3、T4、T5会被锁定，T1和T2保持原状态
    /// </summary>
    [Required(ErrorMessage = "必须指定停止节点")]
    public long StopAfterTaskId { get; set; }
}
