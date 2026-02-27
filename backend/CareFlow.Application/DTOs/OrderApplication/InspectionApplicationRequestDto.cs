using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 检查申请请求DTO
/// </summary>
public class InspectionApplicationRequestDto
{
    /// <summary>
    /// 申请护士ID
    /// </summary>
    [Required(ErrorMessage = "申请护士ID不能为空")]
    public string NurseId { get; set; } = null!;
    
    /// <summary>
    /// 申请任务ID列表（ExecutionTask.Id，签收时生成的申请任务）
    /// </summary>
    [Required(ErrorMessage = "申请任务ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个申请任务")]
    public List<long> TaskIds { get; set; } = new();
    
    /// <summary>
    /// 是否加急
    /// </summary>
    public bool IsUrgent { get; set; }
    
    /// <summary>
    /// 申请备注
    /// </summary>
    [MaxLength(500, ErrorMessage = "备注最多500个字符")]
    public string? Remarks { get; set; }
}
