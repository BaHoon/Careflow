using CareFlow.Application.DTOs.MedicationOrder;
using CareFlow.Application.Interfaces;
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
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<MedicationOrderController> _logger;

    public MedicationOrderController(
        IRepository<MedicationOrder, long> orderRepository,
        IMedicationOrderTaskService taskService,
        INurseAssignmentService nurseAssignmentService,
        ILogger<MedicationOrderController> logger)
    {
        _orderRepository = orderRepository;
        _taskService = taskService;
        _nurseAssignmentService = nurseAssignmentService;
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
            _logger.LogInformation("==================== 开始批量创建医嘱 ====================");
            _logger.LogInformation("患者ID: {PatientId}", request.PatientId);
            _logger.LogInformation("医生ID: {DoctorId}", request.DoctorId);
            _logger.LogInformation("医嘱数量: {Count}", request.Orders.Count);
            
            // 🔥 调试：输出每条医嘱的 Items 信息
            for (int i = 0; i < request.Orders.Count; i++)
            {
                var orderDto = request.Orders[i];
                _logger.LogInformation("医嘱 {Index}: 类型={Type}, Items数量={ItemCount}",
                    i + 1,
                    orderDto.IsLongTerm ? "长期" : "临时",
                    orderDto.Items?.Count ?? 0);
                    
                if (orderDto.Items != null && orderDto.Items.Count > 0)
                {
                    foreach (var item in orderDto.Items)
                    {
                        _logger.LogInformation("  - 药品ID: {DrugId}, 剂量: {Dosage}", 
                            item.DrugId, item.Dosage);
                    }
                }
                else
                {
                    _logger.LogWarning("  ⚠️ 警告: 医嘱 {Index} 的 Items 为空或null!", i + 1);
                }
            }
            _logger.LogInformation("========================================================");

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
            var errors = new List<string>();

            foreach (var orderDto in request.Orders)
            {
                try
                {
                    _logger.LogInformation("🔧 开始处理医嘱: 类型={Type}, Items数量={ItemCount}",
                        orderDto.IsLongTerm ? "长期" : "临时",
                        orderDto.Items?.Count ?? 0);

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
                        // 前端发送的是北京时间+时区信息（如 "2025-12-19T08:00:00+08:00"）
                        // .NET 会自动解析并转换为 UTC 时间存储到数据库
                        TimingStrategy = orderDto.TimingStrategy,
                        StartTime = orderDto.StartTime.HasValue 
                            ? orderDto.StartTime.Value.ToUniversalTime() 
                            : (DateTime?)null,
                        PlantEndTime = orderDto.PlantEndTime.ToUniversalTime(),
                        IntervalHours = orderDto.IntervalHours,
                        IntervalDays = orderDto.IntervalDays,
                        SmartSlotsMask = orderDto.SmartSlotsMask,
                        
                        // 给药途径
                        UsageRoute = (UsageRoute)orderDto.UsageRoute,
                        
                        Remarks = string.IsNullOrWhiteSpace(orderDto.Remarks) ? null : orderDto.Remarks,
                        
                        // 🔥 关键修复：添加 Items 集合
                        Items = new List<MedicationOrderItem>()
                    };

                    // 🔥 关键修复：创建 MedicationOrderItem 实体
                    if (orderDto.Items != null && orderDto.Items.Count > 0)
                    {
                        _logger.LogInformation("💊 开始创建 {Count} 个药品项目", orderDto.Items.Count);
                        
                        foreach (var itemDto in orderDto.Items)
                        {
                            var orderItem = new MedicationOrderItem
                            {
                                DrugId = itemDto.DrugId, // DrugId 是 string 类型
                                Dosage = itemDto.Dosage,
                                Note = string.IsNullOrWhiteSpace(itemDto.Note) ? string.Empty : itemDto.Note,
                                CreateTime = DateTime.UtcNow
                            };
                            
                            order.Items.Add(orderItem);
                            
                            _logger.LogInformation("  ✅ 添加药品: DrugId={DrugId}, Dosage={Dosage}, Note={Note}",
                                orderItem.DrugId, orderItem.Dosage, string.IsNullOrEmpty(orderItem.Note) ? "<空>" : orderItem.Note);
                        }
                        
                        _logger.LogInformation("✅ 成功添加 {Count} 个药品项目到医嘱", order.Items.Count);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 警告: 此医嘱没有药品项目 (Items 为空)");
                    }

                    // 2. 保存医嘱（AddAsync已包含SaveChangesAsync）
                    // EF Core 会自动级联保存 Items 集合
                    _logger.LogInformation("💾 保存医嘱到数据库...");
                    await _orderRepository.AddAsync(order);

                    _logger.LogInformation("✅ 成功创建医嘱，ID: {OrderId}, Items数量: {ItemCount}",
                        order.Id, order.Items?.Count ?? 0);
                    createdOrderIds.Add(order.Id.ToString());
                    
                    // 🏥 计算并设置负责护士（根据排班表）
                    try
                    {
                        var responsibleNurseId = await _nurseAssignmentService.CalculateResponsibleNurseAsync(
                            request.PatientId, 
                            order.StartTime ?? DateTime.UtcNow);
                        
                        if (!string.IsNullOrEmpty(responsibleNurseId))
                        {
                            order.NurseId = responsibleNurseId;
                            await _orderRepository.UpdateAsync(order);
                            _logger.LogInformation("✅ 已分配负责护士: {NurseId} 给医嘱 {OrderId}",
                                responsibleNurseId, order.Id);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ 未找到负责护士，医嘱 {OrderId} 的 NurseId 将保持为空", order.Id);
                        }
                    }
                    catch (Exception nurseEx)
                    {
                        _logger.LogError(nurseEx, "❌ 计算负责护士失败，医嘱 {OrderId}", order.Id);
                        // 护士分配失败不影响医嘱创建，继续执行
                    }
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
                    : $"成功创建{createdOrderIds.Count}条医嘱",
                Data = new BatchCreateOrderDataDto
                {
                    CreatedCount = createdOrderIds.Count,
                    OrderIds = createdOrderIds,
                    TaskCount = 0
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
