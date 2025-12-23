using System.ComponentModel.DataAnnotations;

namespace CareFlow.Application.DTOs.OrderApplication;

/// <summary>
/// 获取申请列表请求DTO
/// </summary>
public class GetApplicationListRequestDto
{
    /// <summary>
    /// 申请类型：Medication | Inspection
    /// </summary>
    [Required(ErrorMessage = "申请类型不能为空")]
    public string ApplicationType { get; set; } = null!;
    
    /// <summary>
    /// 患者ID列表（支持多选）
    /// </summary>
    [Required(ErrorMessage = "患者ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个患者")]
    public List<string> PatientIds { get; set; } = new();
    
    /// <summary>
    /// 状态筛选：Applying | Applied | AppliedConfirmed
    /// 为空表示查询所有状态
    /// </summary>
    public List<string>? StatusFilter { get; set; }
    
    /// <summary>
    /// 时间范围开始（仅药品申请使用）
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 时间范围结束（仅药品申请使用）
    /// </summary>
    public DateTime? EndTime { get; set; }
}
