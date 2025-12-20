using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.SurgicalOrders;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 手术医嘱服务接口
/// </summary>
public interface ISurgicalOrderService
{
    /// <summary>
    /// 批量创建手术医嘱
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建结果</returns>
    Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(BatchCreateSurgicalOrderRequestDto request);
}
