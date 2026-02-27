namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 护士拒绝停嘱请求DTO
/// 用于护士拒绝医生下达的停嘱指令，医嘱将继续执行
/// </summary>
public class RejectStopOrderRequestDto
{
    /// <summary>
    /// 操作护士ID
    /// </summary>
    public string NurseId { get; set; } = null!;
    
    /// <summary>
    /// 待拒绝的停嘱医嘱ID列表
    /// </summary>
    public List<long> OrderIds { get; set; } = new();
    
    /// <summary>
    /// 拒绝停嘱的原因（必填）
    /// 例如："患者症状未缓解，建议继续用药"
    /// </summary>
    public string RejectReason { get; set; } = null!;
}
