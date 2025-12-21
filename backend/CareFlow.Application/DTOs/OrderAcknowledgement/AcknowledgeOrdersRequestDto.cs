namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 批量签收医嘱请求DTO
/// </summary>
public class AcknowledgeOrdersRequestDto
{
    /// <summary>
    /// 签收护士ID
    /// </summary>
    public string NurseId { get; set; } = null!;
    
    /// <summary>
    /// 待签收医嘱ID列表（支持批量）
    /// </summary>
    public List<long> OrderIds { get; set; } = new();
}
