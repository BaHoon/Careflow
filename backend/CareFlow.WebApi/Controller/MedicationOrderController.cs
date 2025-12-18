using CareFlow.Application.DTOs.MedicationOrder;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicationOrderController : ControllerBase
{
    private readonly IRepository<MedicationOrder, long> _orderRepository;
    private readonly IMedicationOrderTaskService _taskService;
    private readonly ILogger<MedicationOrderController> _logger;

    public MedicationOrderController(
        IRepository<MedicationOrder, long> orderRepository,
        IMedicationOrderTaskService taskService,
        ILogger<MedicationOrderController> logger)
    {
        _orderRepository = orderRepository;
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建药物医嘱
    /// </summary>
    [HttpPost("batch-create")]
    public async Task<ActionResult<BatchCreateOrderResponseDto>> BatchCreateOrders(
        [FromBody] BatchCreateOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("开始批量创建医嘱，患者ID: {PatientId}, 医嘱数量: {Count}", 
                request.PatientId, request.Orders.Count);

            if (request.Orders == null || request.Orders.Count == 0)
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "医嘱列表不能为空",
                    Errors = new List<string> { "orders字段不能为空" }
                });
            }

            var createdOrderIds = new List<string>();
            var totalTaskCount = 0;
            var errors = new List<string>();

            foreach (var orderDto in request.Orders)
            {
                try
                {
                    // 1. 创建MedicationOrder实体
                    var order = new MedicationOrder
                    {
                        PatientId = request.PatientId,
                        DoctorId = request.DoctorId,
                        OrderType = "MedicationOrder",
                        IsLongTerm = orderDto.IsLongTerm,
                        Status = "Active",
                        CreateTime = DateTime.UtcNow,
                        
                        // 时间策略字段
                        TimingStrategy = orderDto.TimingStrategy,
                        StartTime = orderDto.StartTime,
                        PlantEndTime = orderDto.PlantEndTime,
                        IntervalHours = orderDto.IntervalHours,
                        IntervalDays = orderDto.IntervalDays,
                        SmartSlotsMask = orderDto.SmartSlotsMask,
                        
                        // 给药途径
                        UsageRoute = (UsageRoute)orderDto.UsageRoute,
                        
                        Remarks = orderDto.Remarks
                    };

                    // 2. 保存医嘱（AddAsync已包含SaveChangesAsync）
                    await _orderRepository.AddAsync(order);

                    _logger.LogInformation("成功创建医嘱，ID: {OrderId}", order.Id);
                    createdOrderIds.Add(order.Id.ToString());

                    // 3. 生成执行任务
                    var tasks = await _taskService.GenerateExecutionTasksAsync(order);
                    totalTaskCount += tasks.Count;

                    _logger.LogInformation("为医嘱 {OrderId} 生成了 {TaskCount} 个执行任务", 
                        order.Id, tasks.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建单个医嘱失败");
                    errors.Add($"医嘱创建失败: {ex.Message}");
                }
            }

            var response = new BatchCreateOrderResponseDto
            {
                Success = createdOrderIds.Count > 0,
                Message = errors.Count > 0 
                    ? $"成功创建{createdOrderIds.Count}条医嘱，{errors.Count}条失败"
                    : $"成功创建{createdOrderIds.Count}条医嘱，生成{totalTaskCount}个执行任务",
                Data = new BatchCreateOrderDataDto
                {
                    CreatedCount = createdOrderIds.Count,
                    OrderIds = createdOrderIds,
                    TaskCount = totalTaskCount
                },
                Errors = errors.Count > 0 ? errors : null
            };

            _logger.LogInformation("批量创建医嘱完成，成功: {Success}, 失败: {Failed}", 
                createdOrderIds.Count, errors.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建医嘱失败");
            return StatusCode(500, new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "批量创建医嘱失败: " + ex.Message,
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
