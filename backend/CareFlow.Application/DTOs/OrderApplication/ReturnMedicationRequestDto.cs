using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 申请退药请求DTO
/// </summary>
public class ReturnMedicationRequestDto
{
    /// <summary>
    /// 操作护士ID
    /// </summary>
    [Required(ErrorMessage = "护士ID不能为空")]
    public string NurseId { get; set; } = null!;
    
    /// <summary>
    /// 退药原因
    /// </summary>
    [Required(ErrorMessage = "退药原因不能为空")]
    [StringLength(500, ErrorMessage = "退药原因不能超过500个字符")]
    public string Reason { get; set; } = null!;
}
