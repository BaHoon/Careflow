using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.OperationOrders;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 操作医嘱控制器（预留）
/// </summary>
[ApiController]
[Route("api/orders/operation")]
public class OperationOrderController : ControllerBase
{
    private readonly IOperationOrderService _operationOrderService;
    private readonly ILogger<OperationOrderController> _logger;

    public OperationOrderController(
        IOperationOrderService operationOrderService,
        ILogger<OperationOrderController> logger)
    {
        _operationOrderService = operationOrderService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建操作医嘱
    /// TODO: 待实现完整功能
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchCreateOrderResponseDto>> BatchCreate(
        [FromBody] BatchCreateOperationOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("接收到批量创建操作医嘱请求: PatientId={PatientId}, DoctorId={DoctorId}, Count={Count}",
                request.PatientId, request.DoctorId, request.Orders.Count);

            var result = await _operationOrderService.BatchCreateOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 操作医嘱创建成功: {Count} 条", result.Data?.CreatedCount);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 操作医嘱创建失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建操作医嘱时发生异常");
            return StatusCode(500, new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
