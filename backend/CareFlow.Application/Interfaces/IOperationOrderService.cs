using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.OperationOrders;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 操作医嘱服务接口
/// </summary>
public interface IOperationOrderService
{
    /// <summary>
    /// 批量创建操作医嘱
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建结果</returns>
    Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(BatchCreateOperationOrderRequestDto request);
}
