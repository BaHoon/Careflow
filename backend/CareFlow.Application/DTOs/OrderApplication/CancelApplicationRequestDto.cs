using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 撤销申请请求DTO
/// </summary>
public class CancelApplicationRequestDto
{
    /// <summary>
    /// 操作护士ID
    /// </summary>
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = null!;
    
    /// <summary>
    /// 要撤销的ID列表（任务ID或医嘱ID）
    /// </summary>
    [Required(ErrorMessage = "ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个项目")]
    public List<long> Ids { get; set; } = new();
    
    /// <summary>
    /// 撤销原因
    /// </summary>
    [MaxLength(500, ErrorMessage = "原因最多500个字符")]
    public string? Reason { get; set; }
}
