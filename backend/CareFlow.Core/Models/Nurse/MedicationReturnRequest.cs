using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Nursing;

/// <summary>
/// 退药申请记录表
/// 用于管理药品任务的退药流程和审计历史
/// </summary>
public class MedicationReturnRequest : EntityBase<long>
{
    /// <summary>
    /// 关联的执行任务ID
    /// </summary>
    public long ExecutionTaskId { get; set; }
    [ForeignKey("ExecutionTaskId")]
    public ExecutionTask ExecutionTask { get; set; } = null!;
    
    /// <summary>
    /// 退药类型
    /// ManualCancel - 护士主动撤销/退药
    /// OrderStopped - 医嘱停止导致的退药
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;
    
    /// <summary>
    /// 申请护士ID
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    [ForeignKey("RequestedBy")]
    public Nurse RequestedByNurse { get; set; } = null!;
    
    /// <summary>
    /// 申请时间
    /// </summary>
    public DateTime RequestedAt { get; set; }
    
    /// <summary>
    /// 退药原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 退药申请状态
    /// Pending - 待提交（护士创建但未提交到药房）
    /// Submitted - 已提交（已调用药房接口，等待确认）
    /// Confirmed - 已确认（药房确认退药成功）
    /// Failed - 失败（药房拒绝或接口失败）
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// 提交到药房的时间
    /// </summary>
    public DateTime? SubmittedAt { get; set; }
    
    /// <summary>
    /// 药房确认时间
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
    
    /// <summary>
    /// 药房确认结果/失败原因
    /// </summary>
    public string? PharmacyResponse { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
