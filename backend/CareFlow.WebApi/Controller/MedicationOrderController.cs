using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.MedicationOrders;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 药物医嘱控制器（重构版 - 使用分离接口方案）
/// </summary>
[ApiController]
[Route("api/orders/medication")]
public class MedicationOrderController : ControllerBase
{
    private readonly IMedicationOrderService _medicationOrderService;
    private readonly ILogger<MedicationOrderController> _logger;

    public MedicationOrderController(
        IMedicationOrderService medicationOrderService,
        ILogger<MedicationOrderController> logger)
    {
        _medicationOrderService = medicationOrderService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建药物医嘱
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchCreateOrderResponseDto>> BatchCreate(
        [FromBody] BatchCreateMedicationOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("接收到批量创建药物医嘱请求: PatientId={PatientId}, DoctorId={DoctorId}, Count={Count}",
                request.PatientId, request.DoctorId, request.Orders.Count);

            // 委托给Service层处理业务逻辑
            var result = await _medicationOrderService.BatchCreateOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 药物医嘱创建成功: {Count} 条", result.Data?.CreatedCount);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 药物医嘱创建部分失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建药物医嘱时发生异常");
            return StatusCode(500, new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 验证医嘱数据（可选，用于前端实时校验）
    /// </summary>
    [HttpPost("validate")]
    public ActionResult<object> ValidateOrder([FromBody] MedicationOrderDto orderDto)
    {
        try
        {
            var errors = new List<object>();
            var warnings = new List<object>();

            // 基础验证
            if (string.IsNullOrWhiteSpace(orderDto.TimingStrategy))
            {
                errors.Add(new { field = "timingStrategy", message = "执行策略不能为空" });
            }

            if (orderDto.PlantEndTime == default)
            {
                errors.Add(new { field = "plantEndTime", message = "医嘱结束时间不能为空" });
            }

            // 策略特定验证
            switch (orderDto.TimingStrategy?.ToUpper())
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

            // 药品验证
            if (orderDto.Items == null || orderDto.Items.Count == 0)
            {
                errors.Add(new { field = "items", message = "至少需要添加一个药品" });
            }
            else
            {
                for (int i = 0; i < orderDto.Items.Count; i++)
                {
                    var item = orderDto.Items[i];
                    if (string.IsNullOrWhiteSpace(item.DrugId))
                    {
                        errors.Add(new { field = $"items[{i}].drugId", message = "药品ID不能为空" });
                    }
                    if (string.IsNullOrWhiteSpace(item.Dosage))
                    {
                        errors.Add(new { field = $"items[{i}].dosage", message = "剂量不能为空" });
                    }
                }
            }

            // TODO: 添加更多验证，如药物相互作用、过敏史检查等

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
