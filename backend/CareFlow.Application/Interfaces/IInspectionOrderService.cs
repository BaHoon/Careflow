using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.InspectionOrders;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 检查医嘱服务接口
/// </summary>
public interface IInspectionOrderService
{
    /// <summary>
    /// 批量创建检查医嘱
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建结果</returns>
    Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(BatchCreateInspectionOrderRequestDto request);
}
