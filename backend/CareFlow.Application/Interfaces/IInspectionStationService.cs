using CareFlow.Application.DTOs.OrderApplication;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 检查站系统集成接口（外部系统）
/// </summary>
public interface IInspectionStationService
{
    /// <summary>
    /// 发送检查申请到检查站（RIS/LIS）
    /// </summary>
    /// <param name="orderIds">检查医嘱ID列表</param>
    /// <param name="isUrgent">是否加急</param>
    /// <returns>检查站系统响应</returns>
    Task<InspectionRequestResult> SendInspectionRequestAsync(
        List<long> orderIds, bool isUrgent);
    
    /// <summary>
    /// 取消检查申请
    /// </summary>
    /// <param name="orderIds">医嘱ID列表</param>
    /// <returns>是否成功</returns>
    Task<bool> CancelInspectionRequestAsync(List<long> orderIds);
}
