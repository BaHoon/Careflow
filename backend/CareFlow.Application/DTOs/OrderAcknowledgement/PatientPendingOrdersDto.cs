namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 患者待签收医嘱详情DTO
/// </summary>
public class PatientPendingOrdersDto
{
    /// <summary>
    /// 新开医嘱列表
    /// </summary>
    public List<PendingOrderDto> NewOrders { get; set; } = new();
    
    /// <summary>
    /// 停止医嘱列表
    /// </summary>
    public List<PendingOrderDto> StoppedOrders { get; set; } = new();
}
