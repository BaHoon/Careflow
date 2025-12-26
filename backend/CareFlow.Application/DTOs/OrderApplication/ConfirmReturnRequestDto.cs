using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 确认退药请求DTO
/// </summary>
public class ConfirmReturnRequestDto
{
    /// <summary>
    /// 操作护士ID
    /// </summary>
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = null!;
}
