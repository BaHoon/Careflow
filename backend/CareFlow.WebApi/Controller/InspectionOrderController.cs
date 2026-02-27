using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.InspectionOrders;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 检查医嘱控制器（预留）
/// </summary>
[ApiController]
[Route("api/orders/inspection")]
public class InspectionOrderController : ControllerBase
{
    private readonly IInspectionOrderService _inspectionOrderService;
    private readonly ILogger<InspectionOrderController> _logger;

    public InspectionOrderController(
        IInspectionOrderService inspectionOrderService,
        ILogger<InspectionOrderController> logger)
    {
        _inspectionOrderService = inspectionOrderService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建检查医嘱 DONE
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchCreateOrderResponseDto>> BatchCreate(
        [FromBody] BatchCreateInspectionOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("接收到批量创建检查医嘱请求: PatientId={PatientId}, DoctorId={DoctorId}, Count={Count}",
                request.PatientId, request.DoctorId, request.Orders.Count);

            var result = await _inspectionOrderService.BatchCreateOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 检查医嘱创建成功: {Count} 条", result.Data?.CreatedCount);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 检查医嘱创建失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建检查医嘱时发生异常");
            return StatusCode(500, new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
