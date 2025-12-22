using CareFlow.Application.DTOs.OrderApplication;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 医嘱申请服务接口
/// </summary>
public interface IOrderApplicationService
{
    /// <summary>
    /// 获取药品申请列表
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>申请项列表</returns>
    Task<List<ApplicationItemDto>> GetMedicationApplicationsAsync(
        GetApplicationListRequestDto request);
    
    /// <summary>
    /// 获取检查申请列表
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>申请项列表</returns>
    Task<List<ApplicationItemDto>> GetInspectionApplicationsAsync(
        GetApplicationListRequestDto request);
    
    /// <summary>
    /// 提交药品申请（批量）
    /// </summary>
    /// <param name="request">申请请求</param>
    /// <returns>申请结果</returns>
    Task<ApplicationResponseDto> SubmitMedicationApplicationAsync(
        MedicationApplicationRequestDto request);
    
    /// <summary>
    /// 提交检查申请（批量）
    /// </summary>
    /// <param name="request">申请请求</param>
    /// <returns>申请结果</returns>
    Task<ApplicationResponseDto> SubmitInspectionApplicationAsync(
        InspectionApplicationRequestDto request);
    
    /// <summary>
    /// 撤销药品申请
    /// </summary>
    /// <param name="taskIds">任务ID列表</param>
    /// <param name="nurseId">操作护士ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>撤销结果</returns>
    Task<ApplicationResponseDto> CancelMedicationApplicationAsync(
        List<long> taskIds, string nurseId, string? reason = null);
    
    /// <summary>
    /// 撤销检查申请
    /// </summary>
    /// <param name="orderIds">医嘱ID列表</param>
    /// <param name="nurseId">操作护士ID</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>撤销结果</returns>
    Task<ApplicationResponseDto> CancelInspectionApplicationAsync(
        List<long> orderIds, string nurseId, string? reason = null);
}
