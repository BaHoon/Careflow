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
    /// 执行结果（JSON格式）
    /// 根据任务类别，可能包含：测量数值、扫描记录、核对结果等
    /// </summary>
    public string? ResultPayload { get; set; }
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
}
