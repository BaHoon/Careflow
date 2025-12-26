using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.DischargeOrders;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 出院医嘱服务接口
/// </summary>
public interface IDischargeOrderService
{
    /// <summary>
    /// 验证患者是否可以开立出院医嘱
    /// 检查患者所有医嘱的状态是否符合出院条件
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>验证结果，包含阻塞原因和最早可出院时间</returns>
    Task<DischargeOrderValidationResult> ValidateDischargeOrderCreationAsync(string patientId);
    
    /// <summary>
    /// 批量创建出院医嘱（带前置验证）
    /// 创建前会自动验证患者是否符合出院条件
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建结果</returns>
    Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(
        BatchCreateDischargeOrderRequestDto request);
    
    /// <summary>
    /// 验证患者是否可以签收出院医嘱
    /// 检查患者是否存在未签收的停止医嘱
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>验证结果，包含未签收的停止医嘱列表</returns>
    Task<DischargeOrderAcknowledgementValidationResult> ValidateDischargeOrderAcknowledgementAsync(
        string patientId);
}
