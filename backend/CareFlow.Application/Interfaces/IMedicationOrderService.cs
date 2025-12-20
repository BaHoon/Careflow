using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.MedicationOrders;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 药物医嘱服务接口
/// </summary>
public interface IMedicationOrderService
{
    /// <summary>
    /// 批量创建药物医嘱
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建结果</returns>
    Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(BatchCreateMedicationOrderRequestDto request);
}
