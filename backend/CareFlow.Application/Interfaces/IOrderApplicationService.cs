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
    
    /// <summary>
    /// 申请退药（单个任务，AppliedConfirmed状态）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="nurseId">操作护士ID</param>
    /// <param name="reason">退药原因</param>
    /// <returns>退药申请结果</returns>
    Task<ApplicationResponseDto> RequestReturnMedicationAsync(
        long taskId, string nurseId, string? reason = null);
    
    /// <summary>
    /// 确认退药（PendingReturn状态，护士确认执行退药）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="nurseId">操作护士ID</param>
    /// <returns>退药确认结果</returns>
    Task<ApplicationResponseDto> ConfirmReturnMedicationAsync(
        long taskId, string nurseId);
    
    /// <summary>
    /// 确认异常取消退药（PendingReturnCancelled状态，将任务标记为异常状态）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="nurseId">操作护士ID</param>
    /// <returns>确认结果</returns>
    Task<ApplicationResponseDto> ConfirmCancelledReturnAsync(
        long taskId, string nurseId);
}
