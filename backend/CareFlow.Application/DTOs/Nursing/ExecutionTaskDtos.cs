using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.Nursing;

/// <summary>
/// 开始执行任务 DTO
/// </summary>
public class StartExecutionTaskDto
{
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = string.Empty;
}

/// <summary>
/// 完成执行任务 DTO
/// </summary>
public class CompleteExecutionTaskDto
{
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = string.Empty;

    /// <summary>
    /// 执行结果（仅用于ResultPending类任务）
    /// 例如：测量数值、检测数据、皮试结果等
    /// </summary>
    public string? ResultPayload { get; set; }
    
    /// <summary>
    /// 执行备注（所有任务类型都可填写）
    /// 记录执行过程中的备注、观察、特殊情况说明
    /// </summary>
    public string? ExecutionRemarks { get; set; }
}

/// <summary>
/// 取消执行任务 DTO
/// </summary>
public class CancelExecutionTaskDto
{
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = string.Empty;

    [Required(ErrorMessage = "取消理由不能为空")]
    [MinLength(2, ErrorMessage = "取消理由至少2个字符")]
    public string CancelReason { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否需要直接退药（仅对AppliedConfirmed状态有效）
    /// true: 直接改为Incomplete状态
    /// false: 改为PendingReturnCancelled状态（任务异常取消待退药）
    /// </summary>
    public bool NeedReturn { get; set; } = false;
}
/// <summary>
/// 更新执行任务状态 DTO（用于扫码任务页面）
/// </summary>
public class UpdateExecutionTaskStatusDto
{
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = string.Empty;

    [Required(ErrorMessage = "状态不能为空")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 执行结果（JSON格式，可选）
    /// </summary>
    public string? ResultPayload { get; set; }
}