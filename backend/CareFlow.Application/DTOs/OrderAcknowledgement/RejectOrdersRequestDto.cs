namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 退回医嘱请求DTO
/// </summary>
public class RejectOrdersRequestDto
{
    /// <summary>
    /// 护士ID
    /// </summary>
    public string NurseId { get; set; } = null!;
    
    /// <summary>
    /// 待退回医嘱ID列表
    /// </summary>
    public List<long> OrderIds { get; set; } = new();
    
    /// <summary>
    /// 退回原因（必填）
    /// </summary>
    public string RejectReason { get; set; } = null!;
}
