using CareFlow.Application.DTOs.OrderAcknowledgement;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 医嘱签收服务接口
/// 负责护士签收医嘱、退回医嘱、查询待签收医嘱等功能
/// </summary>
public interface IOrderAcknowledgementService
{
    /// <summary>
    /// 获取科室所有患者的未签收医嘱统计（用于红点标注）
    /// </summary>
    /// <param name="deptCode">科室代码</param>
    /// <returns>患者列表及其未签收医嘱数量</returns>
    Task<List<PatientUnacknowledgedSummaryDto>> GetPendingOrdersSummaryAsync(string deptCode);
    
    /// <summary>
    /// 获取指定患者的待签收医嘱（包括新开和停止两类）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>患者的新开医嘱和停止医嘱列表</returns>
    Task<PatientPendingOrdersDto> GetPatientPendingOrdersAsync(string patientId);
    
    /// <summary>
    /// 批量签收医嘱
    /// 签收后会自动调用对应类型医嘱的任务拆分服务，生成执行任务并分配责任护士
    /// </summary>
    /// <param name="request">签收请求（包含护士ID和医嘱ID列表）</param>
    /// <returns>签收结果，包含每条医嘱的任务生成情况</returns>
    Task<AcknowledgeOrdersResponseDto> AcknowledgeOrdersAsync(AcknowledgeOrdersRequestDto request);
    
    /// <summary>
    /// 批量退回医嘱
    /// 将医嘱状态改为Draft或Rejected，并记录退回原因
    /// </summary>
    /// <param name="request">退回请求（包含护士ID、医嘱ID列表和退回原因）</param>
    /// <returns>退回结果</returns>
    Task<RejectOrdersResponseDto> RejectOrdersAsync(RejectOrdersRequestDto request);
    
    /// <summary>
    /// 护士拒绝停嘱
    /// 将医嘱从 PendingStop 状态恢复为 InProgress，
    /// 并将关联的 OrderStopping 状态任务恢复为锁定前的原始状态
    /// </summary>
    /// <param name="request">拒绝停嘱请求（包含护士ID、医嘱ID列表和拒绝原因）</param>
    /// <returns>拒绝停嘱结果，包含恢复的任务列表</returns>
    Task<RejectStopOrderResponseDto> RejectStopOrderAsync(RejectStopOrderRequestDto request);
}
