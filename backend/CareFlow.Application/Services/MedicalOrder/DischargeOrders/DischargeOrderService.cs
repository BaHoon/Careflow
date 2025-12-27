using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.DischargeOrders;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MedicalOrderEntity = CareFlow.Core.Models.Medical.MedicalOrder;

namespace CareFlow.Application.Services.DischargeOrders;

/// <summary>
/// 出院医嘱服务实现
/// </summary>
public class DischargeOrderService : IDischargeOrderService
{
    private readonly IRepository<DischargeOrder, long> _orderRepository;
    private readonly IRepository<MedicalOrderEntity, long> _medicalOrderRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<DischargeOrderService> _logger;

    public DischargeOrderService(
        IRepository<DischargeOrder, long> orderRepository,
        IRepository<MedicalOrderEntity, long> medicalOrderRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        IRepository<ExecutionTask, long> taskRepository,
        INurseAssignmentService nurseAssignmentService,
        ILogger<DischargeOrderService> logger)
    {
        _orderRepository = orderRepository;
        _medicalOrderRepository = medicalOrderRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _taskRepository = taskRepository;
        _nurseAssignmentService = nurseAssignmentService;
        _logger = logger;
    }

    /// <summary>
    /// 验证出院医嘱创建前置条件
    /// </summary>
    public async Task<DischargeOrderValidationResult> ValidateDischargeOrderCreationAsync(string patientId)
    {
        _logger.LogInformation("========== 验证患者 {PatientId} 出院医嘱创建前置条件 ==========", patientId);

        try
        {
            var result = new DischargeOrderValidationResult
            {
                CanCreateDischargeOrder = true,
                BlockedOrders = new List<BlockedOrderDto>(),
                PendingStopOrders = new List<PendingStopOrderDto>(),
                EarliestDischargeTime = DateTime.UtcNow // 默认值
            };

            // 1. 查询患者所有医嘱
            var patientOrders = await _medicalOrderRepository.GetQueryable()
                .Where(o => o.PatientId == patientId)
                .OrderBy(o => o.CreateTime)
                .ToListAsync();

            _logger.LogInformation("患者共有 {Count} 条医嘱", patientOrders.Count);

            // 2. 定义需要排除的任务状态（已终止的任务）
            var excludedTaskStatuses = new[]
            {
                ExecutionTaskStatus.OrderStopping,
                ExecutionTaskStatus.Stopped,
                ExecutionTaskStatus.PendingReturn,
                ExecutionTaskStatus.Completed,
                ExecutionTaskStatus.Incomplete
            };

            // 3. 查询该患者所有未完成的任务
            var patientOrderIds = patientOrders.Select(o => o.Id).ToList();
            var activeTasks = await _taskRepository.GetQueryable()
                .Where(t => patientOrderIds.Contains(t.MedicalOrderId) 
                         && !excludedTaskStatuses.Contains(t.Status))
                .ToListAsync();

            _logger.LogInformation("患者共有 {Count} 个未完成任务", activeTasks.Count);

            // 4. 找出有未完成任务的医嘱ID
            var ordersWithActiveTasks = activeTasks
                .Select(t => t.MedicalOrderId)
                .Distinct()
                .ToHashSet();

            // 5. 找出所有未签收的医嘱
            var pendingReceiveOrders = patientOrders
                .Where(o => o.Status == OrderStatus.PendingReceive)
                .ToList();

            _logger.LogInformation("患者有 {Count} 条未签收医嘱", pendingReceiveOrders.Count);

            // 6. 阻塞医嘱 = 有未完成任务的医嘱 + 未签收医嘱
            var blockedOrdersList = new List<MedicalOrderEntity>();

            // 添加有未完成任务的医嘱
            blockedOrdersList.AddRange(patientOrders.Where(o => ordersWithActiveTasks.Contains(o.Id)));

            // 添加未签收医嘱（去重）
            foreach (var order in pendingReceiveOrders)
            {
                if (!blockedOrdersList.Any(b => b.Id == order.Id))
                {
                    blockedOrdersList.Add(order);
                }
            }

            // 注意：阻塞医嘱只是警告信息，不影响是否可以创建出院医嘱
            // 只有时间不符合要求时才真正阻止创建
            if (blockedOrdersList.Any())
            {
                foreach (var order in blockedOrdersList)
                {
                    var hasActiveTasks = ordersWithActiveTasks.Contains(order.Id);
                    var isPendingReceive = order.Status == OrderStatus.PendingReceive;
                    
                    var blockReason = new List<string>();
                    if (hasActiveTasks)
                    {
                        var taskCount = activeTasks.Count(t => t.MedicalOrderId == order.Id);
                        blockReason.Add($"有{taskCount}个未完成任务");
                    }
                    if (isPendingReceive)
                    {
                        blockReason.Add("未签收");
                    }

                    // 加载医嘱详细信息
                    var (itemName, operationName, surgeryName, medicationItems) = await LoadOrderDetailsAsync(order);

                    result.BlockedOrders.Add(new BlockedOrderDto
                    {
                        OrderId = order.Id,
                        OrderType = order.OrderType,
                        Status = order.Status.ToString(),
                        StatusDisplay = $"{GetStatusDisplayName(order.Status)} ({string.Join("、", blockReason)})",
                        Summary = GetOrderSummary(order),
                        CreateTime = order.CreateTime,
                        StartTime = order.CreateTime,
                        EndTime = order.PlantEndTime,
                        ItemName = itemName,
                        OperationName = operationName,
                        SurgeryName = surgeryName,
                        MedicationOrderItems = medicationItems
                    });
                }

                _logger.LogWarning("存在 {Count} 条阻塞医嘱（警告信息）", blockedOrdersList.Count);
            }

            // 7. 计算最早出院时间 = max(当前时间, 未完成任务最晚执行时间, 未签收医嘱计划结束时间)
            var timeConstraints = new List<DateTime> { DateTime.UtcNow };

            // 添加未完成任务的最晚执行时间
            if (activeTasks.Any())
            {
                var latestTaskTime = activeTasks.Max(t => t.PlannedStartTime);
                timeConstraints.Add(latestTaskTime);
                
                _logger.LogInformation("未完成任务最晚执行时间: {Time}", latestTaskTime);
                
                // 记录有未完成任务的医嘱详情（用于PendingStopOrders展示）
                var ordersWithTasksGrouped = activeTasks
                    .GroupBy(t => t.MedicalOrderId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.PlannedStartTime).First());

                foreach (var kvp in ordersWithTasksGrouped)
                {
                    var order = patientOrders.FirstOrDefault(o => o.Id == kvp.Key);
                    if (order != null)
                    {
                        result.PendingStopOrders.Add(new PendingStopOrderDto
                        {
                            OrderId = order.Id,
                            OrderType = order.OrderType,
                            Summary = GetOrderSummary(order),
                            LatestTaskPlannedTime = kvp.Value.PlannedStartTime
                        });
                    }
                }
            }

            // 添加未签收医嘱的计划结束时间
            if (pendingReceiveOrders.Any())
            {
                var latestPendingReceiveEndTime = pendingReceiveOrders.Max(o => o.PlantEndTime);
                timeConstraints.Add(latestPendingReceiveEndTime);
                
                _logger.LogInformation("未签收医嘱最晚计划结束时间: {Time}", latestPendingReceiveEndTime);
            }

            // 取最大值作为最早出院时间
            result.EarliestDischargeTime = timeConstraints.Max();
            _logger.LogInformation("计算得出最早出院时间: {Time}", result.EarliestDischargeTime);

            if (result.CanCreateDischargeOrder)
            {
                _logger.LogInformation("✅ 验证通过，可以创建出院医嘱");
            }
            else
            {
                _logger.LogWarning("❌ 验证失败，存在阻塞医嘱");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 验证出院医嘱创建前置条件失败");
            throw;
        }
    }

    /// <summary>
    /// 批量创建出院医嘱
    /// </summary>
    public async Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(
        BatchCreateDischargeOrderRequestDto request)
    {
        _logger.LogInformation("==================== 开始创建出院医嘱 ====================");
        _logger.LogInformation("患者ID: {PatientId}, 医生ID: {DoctorId}, 出院类型: {DischargeType}",
            request.PatientId, request.DoctorId, request.Orders[0].DischargeType);

        // 1. 前置验证
        var validation = await ValidateDischargeOrderCreationAsync(request.PatientId);
        
        if (!validation.CanCreateDischargeOrder)
        {
            _logger.LogWarning("❌ 前置验证失败，无法创建出院医嘱");
            
            var errorMessages = new List<string>();
            if (validation.BlockedOrders.Any())
            {
                errorMessages.Add($"存在 {validation.BlockedOrders.Count} 条未完成的医嘱，必须先停止或完成所有医嘱");
                foreach (var blocked in validation.BlockedOrders.Take(5)) // 只显示前5条
                {
                    errorMessages.Add($"  - {blocked.Summary} (状态: {blocked.StatusDisplay})");
                }
            }

            return new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "出院医嘱创建前置条件不满足",
                Errors = errorMessages,
                Data = null
            };
        }

        // 2. 验证出院时间
        var orderDto = request.Orders[0];
        if (orderDto.DischargeTime < validation.EarliestDischargeTime)
        {
            _logger.LogWarning("❌ 出院时间早于最早允许时间");
            
            return new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = $"出院时间不能早于 {validation.EarliestDischargeTime:yyyy-MM-dd HH:mm}",
                Errors = new List<string>
                {
                    $"存在待停止医嘱的任务计划执行时间晚于所选出院时间",
                    $"最早允许出院时间: {validation.EarliestDischargeTime:yyyy-MM-dd HH:mm:ss}"
                },
                Data = null
            };
        }

        try
        {
            // 3. 创建出院医嘱实体
            // 先转换出院时间为 UTC
            var dischargeTimeUtc = orderDto.DischargeTime.Kind == DateTimeKind.Utc 
                ? orderDto.DischargeTime 
                : orderDto.DischargeTime.ToUniversalTime();
            
            var order = new DischargeOrder
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                OrderType = "DischargeOrder",
                Status = OrderStatus.PendingReceive,
                IsLongTerm = false, // 出院医嘱固定为临时医嘱
                CreateTime = DateTime.UtcNow,
                // 出院医嘱的计划结束时间就是出院时间
                PlantEndTime = dischargeTimeUtc,
                
                // 出院相关字段
                DischargeType = (DischargeType)orderDto.DischargeType,
                // 将前端传来的时间转换为 UTC（PostgreSQL 要求）
                DischargeTime = dischargeTimeUtc,
                DischargeDiagnosis = orderDto.DischargeDiagnosis,
                DischargeInstructions = orderDto.DischargeInstructions,
                MedicationInstructions = orderDto.MedicationInstructions,
                RequiresFollowUp = orderDto.RequiresFollowUp,
                // FollowUpDate 也需要转换为 UTC
                FollowUpDate = orderDto.FollowUpDate.HasValue
                    ? (orderDto.FollowUpDate.Value.Kind == DateTimeKind.Utc
                        ? orderDto.FollowUpDate.Value
                        : orderDto.FollowUpDate.Value.ToUniversalTime())
                    : (DateTime?)null
            };

            // 4. 添加带回药品（可选）
            var items = orderDto.Items;
            if (items?.Any() == true)
            {
                _logger.LogInformation("出院医嘱包含 {Count} 项带回药品", items.Count);
                
                order.Items = items.Select(itemDto => new MedicationOrderItem
                {
                    DrugId = itemDto.DrugId,
                    Dosage = itemDto.Dosage,
                    Note = itemDto.Note ?? string.Empty
                }).ToList();
            }
            else
            {
                _logger.LogInformation("出院医嘱不包含带回药品");
                order.Items = new List<MedicationOrderItem>();
            }

            // 5. 保存医嘱
            await _orderRepository.AddAsync(order);

            // 6. 插入初始状态历史记录
            var history = new MedicalOrderStatusHistory
            {
                MedicalOrderId = order.Id,
                FromStatus = OrderStatus.Draft,
                ToStatus = OrderStatus.PendingReceive,
                ChangedAt = DateTime.UtcNow,
                ChangedById = request.DoctorId,
                ChangedByType = "Doctor",
                Reason = "医生创建出院医嘱"
            };
            await _statusHistoryRepository.AddAsync(history);

            _logger.LogInformation("✅ 成功创建出院医嘱，ID: {OrderId}", order.Id);

            // 7. 分配负责护士
            await AssignResponsibleNurseAsync(order, request.PatientId);

            return new BatchCreateOrderResponseDto
            {
                Success = true,
                Message = "出院医嘱创建成功",
                Data = new BatchCreateOrderDataDto
                {
                    CreatedCount = 1,
                    OrderIds = new List<string> { order.Id.ToString() },
                    TaskCount = 0 // 任务在签收后生成
                },
                Errors = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建出院医嘱失败");
            
            return new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "出院医嘱创建失败",
                Errors = new List<string> { ex.Message },
                Data = null
            };
        }
        finally
        {
            _logger.LogInformation("========================================================");
        }
    }

    /// <summary>
    /// 验证出院医嘱签收前置条件
    /// </summary>
    public async Task<DischargeOrderAcknowledgementValidationResult> ValidateDischargeOrderAcknowledgementAsync(
        string patientId)
    {
        _logger.LogInformation("========== 验证患者 {PatientId} 出院医嘱签收前置条件 ==========", patientId);

        try
        {
            var result = new DischargeOrderAcknowledgementValidationResult
            {
                CanAcknowledge = true,
                Reason = string.Empty,
                PendingStopOrderIds = new List<long>(),
                PendingStopOrderDetails = new List<PendingStopOrderDetailDto>(),
                BlockedOrders = new List<BlockedOrderDto>()
            };

            // 1. 查询出院医嘱，获取出院时间
            var dischargeOrder = await _orderRepository.GetQueryable()
                .Where(o => o.PatientId == patientId)
                .OrderByDescending(o => o.CreateTime)
                .FirstOrDefaultAsync();

            if (dischargeOrder == null)
            {
                _logger.LogError("❌ 未找到患者 {PatientId} 的出院医嘱", patientId);
                throw new InvalidOperationException($"未找到患者 {patientId} 的出院医嘱");
            }

            var dischargeTime = dischargeOrder.DischargeTime;
            _logger.LogInformation("出院时间: {DischargeTime}", dischargeTime);

            // 2. 第一步检查：查询未签收的新开医嘱（排除出院医嘱）
            var pendingReceiveOrders = await _medicalOrderRepository.GetQueryable()
                .Where(o => o.PatientId == patientId 
                         && o.Status == OrderStatus.PendingReceive
                         && o.OrderType != "DischargeOrder")
                .ToListAsync();

            // 3. 第一步检查：查询待停止医嘱
            var pendingStopOrders = await _medicalOrderRepository.GetQueryable()
                .Where(o => o.PatientId == patientId && o.Status == OrderStatus.PendingStop)
                .ToListAsync();

            _logger.LogInformation("患者有 {PendingReceiveCount} 条未签收医嘱，{PendingStopCount} 条待停止医嘱",
                pendingReceiveOrders.Count, pendingStopOrders.Count);

            // 4. 优先检查：未签收的新开医嘱和待停止医嘱
            if (pendingReceiveOrders.Any() || pendingStopOrders.Any())
            {
                result.CanAcknowledge = false;

                // 处理未签收的新开医嘱
                foreach (var order in pendingReceiveOrders)
                {
                    result.BlockedOrders.Add(new BlockedOrderDto
                    {
                        OrderId = order.Id,
                        OrderType = order.OrderType,
                        Status = order.Status.ToString(),
                        StatusDisplay = "未签收",
                        Summary = GetOrderSummary(order),
                        CreateTime = order.CreateTime
                    });
                }

                // 处理待停止医嘱
                foreach (var order in pendingStopOrders)
                {
                    result.PendingStopOrderIds.Add(order.Id);
                    result.PendingStopOrderDetails.Add(new PendingStopOrderDetailDto
                    {
                        OrderId = order.Id,
                        OrderType = order.OrderType,
                        Summary = GetOrderSummary(order),
                        StopOrderTime = order.StopOrderTime ?? DateTime.UtcNow,
                        StopReason = order.StopReason ?? "未填写停止原因"
                    });
                }

                // 生成原因说明
                var reasons = new List<string>();
                if (pendingReceiveOrders.Any())
                {
                    reasons.Add($"{pendingReceiveOrders.Count} 条未签收的新开医嘱");
                }
                if (pendingStopOrders.Any())
                {
                    reasons.Add($"{pendingStopOrders.Count} 条待停止医嘱未签收");
                }
                
                result.Reason = $"存在{string.Join("、", reasons)}，必须先签收所有医嘱";
                
                _logger.LogWarning("❌ 存在未签收医嘱，无法签收出院医嘱");
                return result;
            }

            // 5. 第二步检查：查询所有非出院医嘱的其他医嘱
            var patientOrders = await _medicalOrderRepository.GetQueryable()
                .Where(o => o.PatientId == patientId && o.OrderType != "DischargeOrder")
                .ToListAsync();

            if (!patientOrders.Any())
            {
                _logger.LogInformation("✅ 验证通过，患者无其他医嘱，可以签收出院医嘱");
                return result;
            }

            // 6. 查询这些医嘱的所有未完成任务（排除已完成、未完成、待退回、已停止状态）
            var patientOrderIds = patientOrders.Select(o => o.Id).ToList();
            var excludedTaskStatuses = new[]
            {
                ExecutionTaskStatus.Completed,
                ExecutionTaskStatus.Incomplete,
                ExecutionTaskStatus.PendingReturn,
                ExecutionTaskStatus.Stopped
            };

            var activeTasks = await _taskRepository.GetQueryable()
                .Where(t => patientOrderIds.Contains(t.MedicalOrderId) 
                         && !excludedTaskStatuses.Contains(t.Status))
                .ToListAsync();

            _logger.LogInformation("患者共有 {Count} 个活跃任务", activeTasks.Count);

            // 7. 检查是否有任务的计划执行时间在出院时间之后
            var tasksAfterDischarge = activeTasks
                .Where(t => t.PlannedStartTime > dischargeTime)
                .ToList();

            if (tasksAfterDischarge.Any())
            {
                result.CanAcknowledge = false;

                // 按医嘱分组，找出有问题的医嘱
                var ordersWithLateTasksIds = tasksAfterDischarge
                    .Select(t => t.MedicalOrderId)
                    .Distinct()
                    .ToHashSet();

                var ordersWithLateTasks = patientOrders
                    .Where(o => ordersWithLateTasksIds.Contains(o.Id))
                    .ToList();

                foreach (var order in ordersWithLateTasks)
                {
                    var orderTasks = tasksAfterDischarge
                        .Where(t => t.MedicalOrderId == order.Id)
                        .ToList();
                    
                    var latestTaskTime = orderTasks.Max(t => t.PlannedStartTime);

                    result.BlockedOrders.Add(new BlockedOrderDto
                    {
                        OrderId = order.Id,
                        OrderType = order.OrderType,
                        Status = order.Status.ToString(),
                        StatusDisplay = $"有 {orderTasks.Count} 个任务计划执行时间晚于出院时间",
                        Summary = GetOrderSummary(order),
                        CreateTime = order.CreateTime,
                        StartTime = order.CreateTime,
                        EndTime = latestTaskTime
                    });
                }

                result.Reason = $"存在 {ordersWithLateTasks.Count} 条医嘱的任务计划执行时间晚于出院时间（{dischargeTime:yyyy-MM-dd HH:mm}），需要退回医嘱或调整出院时间";
                
                _logger.LogWarning("❌ 存在 {Count} 条医嘱的任务计划执行时间晚于出院时间，无法签收出院医嘱", ordersWithLateTasks.Count);
                return result;
            }

            _logger.LogInformation("✅ 验证通过，可以签收出院医嘱");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 验证出院医嘱签收前置条件失败");
            throw;
        }
    }

    // ==================== 私有辅助方法 ====================

    /// <summary>
    /// 分配负责护士
    /// </summary>
    private async Task AssignResponsibleNurseAsync(DischargeOrder order, string patientId)
    {
        try
        {
            var responsibleNurse = await _nurseAssignmentService
                .CalculateResponsibleNurseAsync(patientId, order.CreateTime);

            if (responsibleNurse != null)
            {
                order.NurseId = responsibleNurse;
                await _orderRepository.UpdateAsync(order);
                _logger.LogInformation("医嘱 {OrderId} 分配负责护士 {NurseId}", order.Id, responsibleNurse);
            }
            else
            {
                _logger.LogWarning("医嘱 {OrderId} 无法分配负责护士，当前时间无排班护士", order.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配负责护士失败，医嘱ID: {OrderId}", order.Id);
            // 不影响主流程
        }
    }

    /// <summary>
    /// 获取医嘱状态显示名称
    /// </summary>
    private string GetStatusDisplayName(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Draft => "草稿",
            OrderStatus.PendingReceive => "待签收",
            OrderStatus.Accepted => "已签收",
            OrderStatus.InProgress => "执行中",
            OrderStatus.Completed => "已完成",
            OrderStatus.StoppingInProgress => "停止中",
            OrderStatus.Stopped => "已停止",
            OrderStatus.Cancelled => "已取消",
            OrderStatus.Rejected => "已退回",
            OrderStatus.PendingStop => "待停止",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// 获取医嘱摘要信息
    /// </summary>
    private string GetOrderSummary(MedicalOrderEntity order)
    {
        // 根据不同医嘱类型返回摘要
        // 这里简化处理，实际可以根据 OrderType 调用不同的摘要逻辑
        return order.OrderType switch
        {
            "MedicationOrder" => $"药物医嘱 (ID: {order.Id})",
            "InspectionOrder" => $"检查医嘱 (ID: {order.Id})",
            "SurgicalOrder" => $"手术医嘱 (ID: {order.Id})",
            "OperationOrder" => $"操作医嘱 (ID: {order.Id})",
            "DischargeOrder" => $"出院医嘱 (ID: {order.Id})",
            _ => $"{order.OrderType} (ID: {order.Id})"
        };
    }
    
    /// <summary>
    /// 加载医嘱详细信息
    /// </summary>
    private async Task<(string? ItemName, string? OperationName, string? SurgeryName, List<CareFlow.Application.DTOs.Patient.MedicationItemDto>? MedicationItems)> 
        LoadOrderDetailsAsync(MedicalOrderEntity order)
    {
        string? itemName = null;
        string? operationName = null;
        string? surgeryName = null;
        List<CareFlow.Application.DTOs.Patient.MedicationItemDto>? medicationItems = null;

        try
        {
            switch (order.OrderType)
            {
                case "InspectionOrder":
                    // 加载检查医嘱详细信息
                    var inspectionOrder = await _medicalOrderRepository.GetQueryable()
                        .OfType<InspectionOrder>()
                        .Where(o => o.Id == order.Id)
                        .FirstOrDefaultAsync();
                    if (inspectionOrder != null)
                    {
                        itemName = inspectionOrder.ItemName;
                    }
                    break;

                case "OperationOrder":
                    // 加载操作医嘱详细信息
                    var operationOrder = await _medicalOrderRepository.GetQueryable()
                        .OfType<OperationOrder>()
                        .Where(o => o.Id == order.Id)
                        .FirstOrDefaultAsync();
                    if (operationOrder != null)
                    {
                        operationName = operationOrder.OperationName;
                    }
                    break;

                case "SurgicalOrder":
                    // 加载手术医嘱详细信息
                    var surgicalOrder = await _medicalOrderRepository.GetQueryable()
                        .OfType<SurgicalOrder>()
                        .Where(o => o.Id == order.Id)
                        .FirstOrDefaultAsync();
                    if (surgicalOrder != null)
                    {
                        surgeryName = surgicalOrder.SurgeryName;
                    }
                    break;

                case "MedicationOrder":
                    // 加载药品医嘱详细信息
                    var medicationOrder = await _medicalOrderRepository.GetQueryable()
                        .OfType<MedicationOrder>()
                        .Include(m => m.Items)
                        .ThenInclude(item => item.Drug)
                        .Where(o => o.Id == order.Id)
                        .FirstOrDefaultAsync();
                    
                    if (medicationOrder?.Items != null && medicationOrder.Items.Any())
                    {
                        medicationItems = medicationOrder.Items
                            .Select(item => new CareFlow.Application.DTOs.Patient.MedicationItemDto
                            {
                                Drug = item.Drug != null ? new CareFlow.Application.DTOs.Patient.DrugInfoDto
                                {
                                    DrugName = item.Drug.TradeName ?? item.Drug.GenericName
                                } : null
                            })
                            .ToList();
                    }
                    break;

                case "DischargeOrder":
                    // 加载出院医嘱详细信息（出院带药清单）
                    var dischargeOrder = await _medicalOrderRepository.GetQueryable()
                        .OfType<DischargeOrder>()
                        .Include(m => m.Items)
                        .ThenInclude(item => item.Drug)
                        .Where(o => o.Id == order.Id)
                        .FirstOrDefaultAsync();
                    
                    if (dischargeOrder?.Items != null && dischargeOrder.Items.Any())
                    {
                        medicationItems = dischargeOrder.Items
                            .Select(item => new CareFlow.Application.DTOs.Patient.MedicationItemDto
                            {
                                Drug = item.Drug != null ? new CareFlow.Application.DTOs.Patient.DrugInfoDto
                                {
                                    DrugName = item.Drug.TradeName ?? item.Drug.GenericName
                                } : null
                            })
                            .ToList();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载医嘱 {OrderId} 详细信息失败", order.Id);
        }

        return (itemName, operationName, surgeryName, medicationItems);
    }
}
