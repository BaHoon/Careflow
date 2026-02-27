using Microsoft.AspNetCore.Mvc;
using CareFlow.Application.DTOs.OperationOrders;
using CareFlow.Application.DTOs.Common;
using CareFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 操作医嘱控制器（参照药品医嘱实现）
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
    /// 批量创建操作医嘱（参照药品医嘱）
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchCreateOrderResponseDto>> BatchCreate(
        [FromBody] BatchCreateOperationOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("接收到批量创建操作医嘱请求: PatientId={PatientId}, DoctorId={DoctorId}, Count={Count}",
                request.PatientId, request.DoctorId, request.Orders.Count);

            // 委托给Service层处理业务逻辑
            var result = await _operationOrderService.BatchCreateOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 操作医嘱创建成功: {Count} 条", result.Data?.CreatedCount);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 操作医嘱创建部分失败: {Message}", result.Message);
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

    /// <summary>
    /// 验证操作医嘱数据（用于前端实时校验，参照药品医嘱）
    /// </summary>
    [HttpPost("validate")]
    public ActionResult<object> ValidateOrder([FromBody] OperationOrderDto orderDto)
    {
        try
        {
            var errors = new List<object>();
            var warnings = new List<object>();

            // 基础验证
            if (string.IsNullOrWhiteSpace(orderDto.OpId))
            {
                errors.Add(new { field = "opId", message = "操作代码不能为空" });
            }

            if (string.IsNullOrWhiteSpace(orderDto.OperationName))
            {
                errors.Add(new { field = "operationName", message = "操作名称不能为空" });
            }

            if (orderDto.PlantEndTime == default)
            {
                errors.Add(new { field = "plantEndTime", message = "医嘱结束时间不能为空" });
            }

            // 时间策略验证
            if (string.IsNullOrWhiteSpace(orderDto.TimingStrategy))
            {
                errors.Add(new { field = "timingStrategy", message = "时间策略不能为空" });
            }
            else
            {
                switch (orderDto.TimingStrategy.ToUpper())
                {
                    case "SPECIFIC":
                        if (orderDto.StartTime == null)
                        {
                            errors.Add(new { field = "startTime", message = "指定时间策略需要设置开始时间" });
                        }
                        else if (orderDto.StartTime < DateTime.UtcNow)
                        {
                            errors.Add(new { field = "startTime", message = "开始时间不能早于当前时间" });
                        }
                        break;

                    case "CYCLIC":
                        if (orderDto.StartTime == null)
                        {
                            errors.Add(new { field = "startTime", message = "周期策略需要设置开始时间" });
                        }
                        if (!orderDto.IntervalHours.HasValue || orderDto.IntervalHours <= 0)
                        {
                            errors.Add(new { field = "intervalHours", message = "周期策略需要设置间隔小时数" });
                        }
                        break;

                    case "SLOTS":
                        if (orderDto.StartTime == null)
                        {
                            errors.Add(new { field = "startTime", message = "时段策略需要设置开始时间" });
                        }
                        if (orderDto.SmartSlotsMask <= 0)
                        {
                            errors.Add(new { field = "smartSlotsMask", message = "时段策略需要选择至少一个时段" });
                        }
                        break;
                }
            }

            // 时间验证
            if (orderDto.PlantEndTime <= DateTime.UtcNow)
            {
                errors.Add(new { field = "plantEndTime", message = "医嘱结束时间不能早于当前时间" });
            }

            // TODO: 添加更多验证，如操作代码有效性、操作要求完整性等

            return Ok(new
            {
                isValid = errors.Count == 0,
                errors,
                warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证医嘱失败");
            return StatusCode(500, new { message = "验证失败: " + ex.Message });
        }
    }
}


