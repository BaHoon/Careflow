using CareFlow.Application.DTOs.OrderApplication;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 药房系统集成接口（外部系统）
/// </summary>
public interface IPharmacyIntegrationService
{
    /// <summary>
    /// 发送取药请求到药房系统
    /// </summary>
    /// <param name="taskIds">取药任务ID列表</param>
    /// <param name="isUrgent">是否加急</param>
    /// <returns>药房系统响应</returns>
    Task<PharmacyRequestResult> SendMedicationRequestAsync(
        List<long> taskIds, bool isUrgent);
    
    /// <summary>
    /// 取消取药请求
    /// </summary>
    /// <param name="taskIds">任务ID列表</param>
    /// <returns>是否成功</returns>
    Task<bool> CancelMedicationRequestAsync(List<long> taskIds);
}
