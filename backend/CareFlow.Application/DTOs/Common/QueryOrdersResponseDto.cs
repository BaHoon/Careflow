namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 查询医嘱响应DTO
/// 返回医嘱列表及统计信息
/// </summary>
public class QueryOrdersResponseDto
{
    /// <summary>
    /// 医嘱列表
    /// </summary>
    public List<OrderSummaryDto> Orders { get; set; } = new();
    
    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; } = string.Empty;
}
