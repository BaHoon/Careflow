using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models;
using CareFlow.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace CareFlow.Application.Services;

public class MedicationOrderTaskService : IMedicationOrderTaskService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<HospitalTimeSlot, int> _timeSlotRepository;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
    private readonly IRepository<MedicationOrder, long> _medicationOrderRepository;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<MedicationOrderTaskService> _logger;

    public MedicationOrderTaskService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<HospitalTimeSlot, int> timeSlotRepository,
        IRepository<BarcodeIndex, string> barcodeRepository,
        IRepository<MedicationOrder, long> medicationOrderRepository,
        IBarcodeService barcodeService,
        ILogger<MedicationOrderTaskService> logger)
    {
        _taskRepository = taskRepository;
        _timeSlotRepository = timeSlotRepository;
        _barcodeRepository = barcodeRepository;
        _medicationOrderRepository = medicationOrderRepository;
        _barcodeService = barcodeService;
        _logger = logger;
    }

    public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(MedicationOrder order)
    {
        // 生成任务拆分逻辑
        // 1. 输入参数验证
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "医嘱对象不能为空");
        }

        _logger.LogInformation("开始为医嘱 {OrderId} 生成执行任务", order.Id);

        // 2. 验证医嘱是否真实存在于数据库中，并加载Items和Drug信息
        var existingOrder = await _medicationOrderRepository.GetQueryable()
            .Include(m => m.Items)
                .ThenInclude(item => item.Drug)
            .FirstOrDefaultAsync(m => m.Id == order.Id);
        if (existingOrder == null)
        {
            var errorMsg = $"医嘱 {order.Id} 在数据库中不存在，无法生成执行任务";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // 3. 验证医嘱状态是否允许生成任务
        if (existingOrder.Status == OrderStatus.Cancelled || existingOrder.Status == OrderStatus.Completed)
        {
            var errorMsg = $"医嘱 {order.Id} 状态为 {existingOrder.Status}，不允许生成执行任务";
            _logger.LogWarning(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // 4. 验证必要字段
        await ValidateOrderFields(existingOrder);

        _logger.LogInformation("医嘱 {OrderId} 验证通过，开始生成执行任务", order.Id);

        // 5. 检查是否已存在未完成的任务
        var hasPendingTasks = await HasPendingTasksAsync(existingOrder.Id);
        if (hasPendingTasks)
        {
            _logger.LogWarning("医嘱 {OrderId} 已存在未完成的任务，建议先处理现有任务", order.Id);
            // 根据业务需求，可以选择抛出异常或继续执行
            throw new InvalidOperationException($"医嘱 {order.Id} 已存在未完成的执行任务");
        }

        var tasks = new List<ExecutionTask>();

        try
        {
            switch (existingOrder.TimingStrategy.ToUpper())
            {
                case "IMMEDIATE":
                    tasks.AddRange(await GenerateImmediateTasks(existingOrder));
                    break;
                case "SPECIFIC":
                    tasks.AddRange(await GenerateSpecificTasks(existingOrder));
                    break;
                case "CYCLIC":
                    tasks.AddRange(await GenerateCyclicTasks(existingOrder));
                    break;
                case "SLOTS":
                    tasks.AddRange(await GenerateSlotsTasks(existingOrder));
                    break;
                default:
                    throw new ArgumentException($"不支持的时间策略: {existingOrder.TimingStrategy}");
            }

            // 批量保存任务到数据库
            if (tasks.Any())
            {
                var savedTaskCount = 0;
                foreach (var task in tasks)
                {
                    try
                    {
                        // 先保存任务以获得ID
                        await _taskRepository.AddAsync(task);
                        savedTaskCount++;
                        
                        // 为任务生成条形码索引
                        await GenerateBarcodeForTask(task);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存执行任务或生成条形码时发生错误，任务计划时间: {PlannedTime}", 
                            task.PlannedStartTime);
                        // 继续处理其他任务，不让单个任务的失败影响整体流程
                    }
                }
                
                _logger.LogInformation("已为医嘱 {OrderId} 成功生成 {SavedCount}/{TotalCount} 个执行任务", 
                    existingOrder.Id, savedTaskCount, tasks.Count);

                if (savedTaskCount == 0)
                {
                    throw new InvalidOperationException("所有执行任务保存失败");
                }
            }
            else
            {
                _logger.LogWarning("医嘱 {OrderId} 未生成任何执行任务，可能是时间条件不满足", existingOrder.Id);
            }

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为医嘱 {OrderId} 生成执行任务时发生错误", order.Id);
            throw;
        }
    }

    public async Task RollbackPendingTasksAsync(long orderId, string reason)
    {
        // 医嘱回滚
        // 输入验证
        if (orderId <= 0)
        {
            throw new ArgumentException("医嘱ID必须大于0", nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("回滚原因不能为空", nameof(reason));
        }

        // 验证医嘱是否存在
        var existingOrder = await _medicationOrderRepository.GetByIdAsync(orderId);
        if (existingOrder == null)
        {
            var errorMsg = $"医嘱 {orderId} 在数据库中不存在，无法回滚任务";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.LogInformation("开始回滚医嘱 {OrderId} 的未执行任务，原因: {Reason}", orderId, reason);

        try
        {
            // 查找所有未开始执行的任务
            var pendingTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                t.Status == "Pending" && 
                t.ActualStartTime == null);

            foreach (var task in pendingTasks)
            {
                task.Status = "Cancelled";
                task.ExceptionReason = $"医嘱停止: {reason}";
                task.IsRolledBack = true;
                task.LastModifiedAt = DateTime.UtcNow;
                
                await _taskRepository.UpdateAsync(task);
            }

            _logger.LogInformation("已回滚医嘱 {OrderId} 的 {TaskCount} 个未执行任务", orderId, pendingTasks.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚医嘱 {OrderId} 的任务时发生错误", orderId);
            throw;
        }
    }

    public async Task RefreshExecutionTasksAsync(MedicationOrder order)
    {
        // 医嘱更新后刷新任务
        // 输入验证
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "医嘱对象不能为空");
        }

        if (order.Id <= 0)
        {
            throw new ArgumentException("医嘱ID必须大于0", nameof(order));
        }

        // 验证医嘱是否存在
        var existingOrder = await _medicationOrderRepository.GetByIdAsync(order.Id);
        if (existingOrder == null)
        {
            var errorMsg = $"医嘱 {order.Id} 在数据库中不存在，无法刷新任务";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.LogInformation("开始刷新医嘱 {OrderId} 的执行任务", order.Id);

        try
        {
            // 1. 回滚所有未执行的任务
            await RollbackPendingTasksAsync(order.Id, "医嘱修改");

            // 2. 重新生成任务
            await GenerateExecutionTasksAsync(order);

            _logger.LogInformation("已刷新医嘱 {OrderId} 的执行任务", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新医嘱 {OrderId} 的任务时发生错误", order.Id);
            throw;
        }
    }

    #region 私有方法 - 各种时间策略的实现

    /// <summary>
    /// 生成立即执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateImmediateTasks(MedicationOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        var executionTime = DateTime.UtcNow;
        var tasks = new List<ExecutionTask>();
        
        // 1. 生成取药任务（必须）
        var retrieveTask = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = TaskCategory.Verification, // 取药为核对类
            PlannedStartTime = executionTime.AddMinutes(-30), // 提前30分钟取药
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            DataPayload = GenerateRetrieveMedicationDataPayload(order, executionTime.AddMinutes(-30))
        };
        
        // 2. 生成给药任务
        var administrationTask = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = GetTaskCategoryFromUsageRoute(order.UsageRoute),
            PlannedStartTime = executionTime,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            DataPayload = GenerateTaskDataPayload(order, executionTime)
        };
        
        tasks.Add(retrieveTask);
        tasks.Add(administrationTask);
        
        return tasks;
    }

    /// <summary>
    /// 生成指定时间执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateSpecificTasks(MedicationOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (!order.StartTime.HasValue)
        {
            throw new ArgumentException("SPECIFIC策略必须指定执行时间（StartTime）");
        }

        // 只生成未来时间的任务
        if (order.StartTime.Value <= DateTime.UtcNow)
        {
            _logger.LogWarning("医嘱 {OrderId} 的执行时间 {StartTime} 已过期，跳过任务生成", 
                order.Id, order.StartTime.Value);
            return new List<ExecutionTask>();
        }

        var executionTime = order.StartTime.Value;
        var tasks = new List<ExecutionTask>();
        
        // 1. 生成取药任务（必须）
        var retrieveTask = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = TaskCategory.Verification, // 取药为核对类
            PlannedStartTime = executionTime.AddMinutes(-30), // 提前30分钟取药
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            DataPayload = GenerateRetrieveMedicationDataPayload(order, executionTime.AddMinutes(-30))
        };
        
        // 2. 生成给药任务
        var administrationTask = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = GetTaskCategoryFromUsageRoute(order.UsageRoute),
            PlannedStartTime = executionTime,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            DataPayload = GenerateTaskDataPayload(order, executionTime)
        };
        
        tasks.Add(retrieveTask);
        tasks.Add(administrationTask);
        
        return tasks;
    }

    /// <summary>
    /// 生成周期性执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateCyclicTasks(MedicationOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        // 验证间隔小时数
        if (!order.IntervalHours.HasValue || order.IntervalHours.Value <= 0)
            throw new ArgumentException("周期性医嘱必须指定有效的执行间隔（小时数必须大于0）");

        if (order.IntervalDays <= 0)
            throw new ArgumentException("周期间隔天数必须大于0");

        var tasks = new List<ExecutionTask>();
        
        // 使用 StartTime 作为首次执行的完整时间（包含日期+时刻）
        var currentExecutionTime = order.StartTime ?? DateTime.UtcNow;
        var endTime = order.PlantEndTime;

        if (endTime < currentExecutionTime)
        {
            _logger.LogWarning("医嘱 {OrderId} 的结束时间早于开始时间，无法生成周期任务", order.Id);
            return tasks;
        }

        var intervalHours = (double)order.IntervalHours.Value;

        // 按时间间隔循环生成任务，直到超过结束时间
        while (currentExecutionTime <= endTime)
        {
            // 只生成未来时间的任务
            if (currentExecutionTime > DateTime.UtcNow)
            {
                // 1. 生成取药任务
                var retrieveTask = new ExecutionTask
                {
                    MedicalOrderId = order.Id,
                    PatientId = order.PatientId,
                    Category = TaskCategory.Verification,
                    PlannedStartTime = currentExecutionTime.AddMinutes(-30),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    DataPayload = GenerateRetrieveMedicationDataPayload(order, currentExecutionTime.AddMinutes(-30))
                };
                
                // 2. 生成给药任务
                var administrationTask = new ExecutionTask
                {
                    MedicalOrderId = order.Id,
                    PatientId = order.PatientId,
                    Category = GetTaskCategoryFromUsageRoute(order.UsageRoute),
                    PlannedStartTime = currentExecutionTime,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    DataPayload = GenerateTaskDataPayload(order, currentExecutionTime)
                };
                
                tasks.Add(retrieveTask);
                tasks.Add(administrationTask);
            }

            // 移动到下一次执行时间
            currentExecutionTime = currentExecutionTime.AddHours(intervalHours);
        }

        return tasks;
    }

    /// <summary>
    /// 生成时段执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateSlotsTasks(MedicationOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (order.SmartSlotsMask <= 0)
            throw new ArgumentException("时段掩码必须大于0");

        var tasks = new List<ExecutionTask>();
        
        // 解析时段位掩码
        var timeSlots = await ParseSlotsMask(order.SmartSlotsMask);
        
        if (!timeSlots.Any())
        {
            _logger.LogWarning("医嘱 {OrderId} 的时段掩码 {SlotsMask} 未匹配到任何有效时段", 
                order.Id, order.SmartSlotsMask);
            return tasks;
        }

        var startDate = (order.StartTime ?? DateTime.UtcNow).Date;
        var endDate = order.PlantEndTime.Date;

        if (endDate < startDate)
        {
            _logger.LogWarning("医嘱 {OrderId} 的结束时间早于开始时间，无法生成时段任务", order.Id);
            return tasks;
        }

        for (var date = startDate; date <= endDate; date = date.AddDays(order.IntervalDays))
        {
            foreach (var slot in timeSlots)
            {
                var executionTime = date.Add(slot.DefaultTime);
                
                // 只生成未来时间的任务
                if (executionTime > DateTime.UtcNow)
                {
                    // 1. 生成取药任务（必须）
                    var retrieveTask = new ExecutionTask
                    {
                        MedicalOrderId = order.Id,
                        PatientId = order.PatientId,
                        Category = TaskCategory.Verification, // 取药为核对类
                        PlannedStartTime = executionTime.AddMinutes(-30), // 提前30分钟取药
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        DataPayload = GenerateRetrieveMedicationDataPayload(order, executionTime.AddMinutes(-30))
                    };
                    
                    // 2. 生成给药任务
                    var administrationTask = new ExecutionTask
                    {
                        MedicalOrderId = order.Id,
                        PatientId = order.PatientId,
                        Category = GetTaskCategoryFromUsageRoute(order.UsageRoute),
                        PlannedStartTime = executionTime,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        DataPayload = GenerateTaskDataPayload(order, executionTime, slot.SlotName)
                    };
                    
                    tasks.Add(retrieveTask);
                    tasks.Add(administrationTask);
                }
            }
        }

        return tasks;
    }

    #endregion

    #region 验证方法

    /// <summary>
    /// 验证医嘱字段的完整性和有效性
    /// </summary>
    private async Task ValidateOrderFields(MedicationOrder order)
    {
        var validationErrors = new List<string>();

        // 检查必要字段
        if (string.IsNullOrWhiteSpace(order.PatientId))
            validationErrors.Add("患者ID不能为空");

        if (string.IsNullOrWhiteSpace(order.DoctorId))
            validationErrors.Add("医生ID不能为空");

        if (order.Items == null || !order.Items.Any())
            throw new ArgumentException($"药品医嘱必须包含至少一种药品（{order.Items?.Count() ?? 0}）");

        if (!Enum.IsDefined(typeof(UsageRoute), order.UsageRoute))
            throw new ArgumentException("给药途径不能为空或无效");
        
        if (string.IsNullOrWhiteSpace(order.TimingStrategy))
            validationErrors.Add("时间策略不能为空");

        // 检查时间字段
        if (order.PlantEndTime <= DateTime.UtcNow.AddMinutes(-5)) // 允许5分钟的时间误差
        {
            validationErrors.Add("医嘱结束时间不能早于当前时间");
        }

        if (order.StartTime.HasValue && order.StartTime.Value > order.PlantEndTime)
        {
            validationErrors.Add("医嘱开始时间不能晚于结束时间");
        }

        // 根据时间策略验证特定字段
        switch (order.TimingStrategy.ToUpper())
        {
            case "SPECIFIC":
                if (!order.StartTime.HasValue)
                    validationErrors.Add("SPECIFIC策略必须指定执行时间（StartTime）");
                else if (order.StartTime.Value <= DateTime.UtcNow)
                    validationErrors.Add("执行时间必须是未来时间");
                break;

            case "SLOTS":
                if (order.SmartSlotsMask <= 0)
                    validationErrors.Add("SLOTS策略必须指定有效的时段掩码");
                else
                {
                    // 验证时段掩码是否对应有效的时间槽位
                    var timeSlots = await ParseSlotsMask(order.SmartSlotsMask);
                    if (!timeSlots.Any())
                        validationErrors.Add("时段掩码未匹配到任何有效的时间槽位");
                }
                break;

            case "CYCLIC":
                if (!order.IntervalHours.HasValue || order.IntervalHours.Value <= 0)
                {
                    validationErrors.Add("CYCLIC策略必须指定有效的执行间隔（小时数必须大于0）");
                }
                else if (order.IntervalHours.Value > 168) // 7天 = 168小时
                {
                    validationErrors.Add("执行间隔不能超过168小时（7天），请合理设置频次");
                }
                
                if (order.IntervalDays <= 0)
                    validationErrors.Add("CYCLIC策略的间隔天数必须大于0");
                break;
        }

        // 如果有验证错误，抛出异常
        if (validationErrors.Any())
        {
            var errorMessage = string.Join("; ", validationErrors);
            _logger.LogError("医嘱 {OrderId} 验证失败: {Errors}", order.Id, errorMessage);
            throw new ArgumentException($"医嘱验证失败: {errorMessage}");
        }

        _logger.LogDebug("医嘱 {OrderId} 字段验证通过", order.Id);
    }

    /// <summary>
    /// 检查是否已存在未完成的任务，避免重复生成
    /// </summary>
    private async Task<bool> HasPendingTasksAsync(long orderId)
    {
        try
        {
            var existingTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == "Pending" || t.Status == "InProgress"));

            var hasPending = existingTasks.Any();
            
            if (hasPending)
            {
                _logger.LogWarning("医嘱 {OrderId} 已存在 {TaskCount} 个未完成的执行任务", 
                    orderId, existingTasks.Count());
            }

            return hasPending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查医嘱 {OrderId} 的待执行任务时发生错误", orderId);
            return false; // 发生错误时不阻止任务生成，但会记录日志
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 生成执行任务的标准化DataPayload
    /// </summary>
    private string GenerateTaskDataPayload(MedicationOrder order, DateTime executionTime, string? slotName = null)
    {
        try
        {
            // 构建药品描述
            var drugDescription = BuildDrugDescription(order);
            
            // 构建使用时间描述
            var timingDescription = BuildTimingDescription(order, executionTime, slotName);
            
            // 创建任务项列表 - 基于Items集合
            var items = new List<object>();
            var itemId = 1;

            // 为每个药品项目创建核对任务
            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    var drugName = item.Drug?.GenericName ?? 
                                  item.Drug?.TradeName ?? 
                                  $"药品({item.DrugId})";
                    items.Add(new
                    {
                        id = itemId++,
                        text = $"核对药品：{drugName} {item.Dosage}",
                        isChecked = false,
                        required = true,
                        drugId = item.DrugId
                    });
                }
            }

            // 添加通用核对项目
            items.Add(new
            {
                id = itemId++,
                text = $"核对给药途径：{order.UsageRoute}",
                isChecked = false,
                required = true
            });

            items.Add(new
            {
                id = itemId++,
                text = $"核对患者身份",
                isChecked = false,
                required = true
            });

            // 构建完整的描述
            var fullDescription = $"{drugDescription} - {order.UsageRoute} - {timingDescription}";

            var dataPayload = new
            {
                TaskType = "MEDICATION_ADMINISTRATION",
                Title = "药品给药核对",
                Description = fullDescription,
                IsChecklist = true,
                Items = items,
                MedicationInfo = new
                {
                    OrderId = order.Id,
                    Items = order.Items?.Select(item => new
                    {
                        DrugId = item.DrugId,
                        DrugName = item.Drug?.GenericName,
                        Dosage = item.Dosage,
                        Note = item.Note
                    }) ?? Enumerable.Empty<object>(),
                    UsageRoute = order.UsageRoute,
                    FrequencyDescription = GetFrequencyDescription(order),
                    ExecutionTime = executionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    SlotName = slotName
                }
            };

            return JsonSerializer.Serialize(dataPayload, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成DataPayload时发生错误，医嘱ID: {OrderId}", order.Id);
            // 返回简化版本，确保不会因为DataPayload生成失败而影响任务创建
            var firstDrugInfo = order.Items?.FirstOrDefault();
            var drugName = firstDrugInfo?.Drug?.GenericName ?? 
                          firstDrugInfo?.Drug?.TradeName ?? 
                          (firstDrugInfo != null ? $"药品({firstDrugInfo.DrugId})" : "未知药品");
            
            return JsonSerializer.Serialize(new
            {
                TaskType = "MEDICATION_ADMINISTRATION",
                Title = "药品给药核对",
                Description = $"药品给药 - {drugName} - {order.UsageRoute}",
                IsChecklist = true,
                Items = new[] { new { id = 1, text = "执行给药任务", isChecked = false, required = true } }
            });
        }
    }

    /// <summary>
    /// 构建药品描述信息
    /// </summary>
    private string BuildDrugDescription(MedicationOrder order)
    {
        if (order.Items == null || !order.Items.Any())
        {
            return "无药品信息";
        }

        if (order.Items.Count == 1)
        {
            // 单个药品
            var item = order.Items.First();
            var drugName = item.Drug?.GenericName ?? item.Drug?.TradeName ?? $"药品({item.DrugId})";
            var tradeName = item.Drug?.TradeName;
            var specification = item.Drug?.Specification;
            
            var fullName = drugName;
            if (!string.IsNullOrEmpty(tradeName) && tradeName != drugName)
            {
                fullName += $"({tradeName})";
            }
            if (!string.IsNullOrEmpty(specification))
            {
                fullName += $" {specification}";
            }
            
            return $"{fullName} {item.Dosage}";
        }
        else
        {
            // 多个药品 - 组合药品
            var descriptions = order.Items.Select(item =>
            {
                var drugName = item.Drug?.GenericName ?? 
                              item.Drug?.TradeName ?? 
                              $"药品({item.DrugId})";
                return $"{drugName} {item.Dosage}";
            });
            
            return $"组合用药：{string.Join(" + ", descriptions)}";
        }
    }

    /// <summary>
    /// 构建时间描述信息
    /// </summary>
    private string BuildTimingDescription(MedicationOrder order, DateTime executionTime, string? slotName = null)
    {
        var timingParts = new List<string>();

        // 添加频次信息（针对CYCLIC策略）
        if (order.TimingStrategy.ToUpper() == "CYCLIC" && order.IntervalHours.HasValue)
        {
            var freqDescription = GetFrequencyDescription(order);
            timingParts.Add(freqDescription);
        }

        // 添加时段信息
        if (!string.IsNullOrEmpty(slotName))
        {
            timingParts.Add($"{slotName}给药");
        }

        // 添加具体执行时间
        timingParts.Add($"执行时间：{executionTime:yyyy-MM-dd HH:mm}");

        // 添加策略类型描述
        switch (order.TimingStrategy.ToUpper())
        {
            case "IMMEDIATE":
                timingParts.Add("立即执行");
                break;
            case "SPECIFIC":
                timingParts.Add("指定时间执行");
                break;
            case "CYCLIC":
                if (order.IntervalDays == 1)
                    timingParts.Add("每日执行");
                else
                    timingParts.Add($"每{order.IntervalDays}天执行");
                break;
            case "SLOTS":
                timingParts.Add("按时段执行");
                break;
        }

        return string.Join("，", timingParts);
    }

    /// <summary>
    /// 获取频次的中文描述（用于显示）
    /// </summary>
    private string GetFrequencyDescription(MedicationOrder order)
    {
        if (!order.IntervalHours.HasValue)
            return "未指定频次";

        var hours = order.IntervalHours.Value;
        
        // 生成友好的描述
        if (hours == 24)
            return "每日一次";
        else if (hours == 12)
            return "每12小时一次（每日2次）";
        else if (hours == 8)
            return "每8小时一次（每日3次）";
        else if (hours == 6)
            return "每6小时一次（每日4次）";
        else if (hours == 4)
            return "每4小时一次（每日6次）";
        else if (hours < 1)
            return $"每{(int)(hours * 60)}分钟一次";
        else if (hours == (int)hours)
            return $"每{(int)hours}小时一次";
        else
            return $"每{hours:F1}小时一次";
    }

    /// <summary>
    /// 计算具体的执行时间
    /// </summary>
    private DateTime CalculateExecutionTime(DateTime date, int executionIndex, int totalExecutions)
    {
        // 根据执行次数平均分布到一天中
        var hoursInterval = 24.0 / totalExecutions;
        var startHour = 8; // 从早上8点开始
        
        var executionHour = startHour + (executionIndex * hoursInterval);
        
        // 确保不超过24小时
        if (executionHour >= 24)
        {
            executionHour -= 24;
            date = date.AddDays(1);
        }
        
        return date.AddHours(executionHour);
    }

    /// <summary>
    /// 解析时段位掩码，获取对应的时间槽位
    /// </summary>
    private async Task<List<HospitalTimeSlot>> ParseSlotsMask(int slotsMask)
    {
        var matchedSlots = new List<HospitalTimeSlot>();
        
        // 获取所有时间槽位
        var allSlots = await _timeSlotRepository.ListAsync();
        
        foreach (var slot in allSlots)
        {
            // 检查位掩码中是否包含这个槽位
            if ((slotsMask & slot.Id) == slot.Id)
            {
                matchedSlots.Add(slot);
            }
        }
        
        // 按默认时间排序
        return matchedSlots.OrderBy(s => s.DefaultTime).ToList();
    }

    /// <summary>
    /// 为执行任务生成条形码索引
    /// </summary>
    private async Task GenerateBarcodeForTask(ExecutionTask task)
    {
        try
        {
            var barcodeIndex = new BarcodeIndex
            {
                Id = $"ExecutionTasks-{task.Id}", // 使用表名和ID作为唯一标识
                TableName = "ExecutionTasks",
                RecordId = task.Id.ToString()
            };

            // 生成条形码并保存到文件系统
            var barcodeResult = await _barcodeService.GenerateAndSaveBarcodeAsync(barcodeIndex, saveToFile: true);
            
            // 更新条形码索引信息
            barcodeIndex.ImagePath = barcodeResult.FilePath;
            barcodeIndex.ImageSize = barcodeResult.FileSize;
            barcodeIndex.ImageMimeType = barcodeResult.MimeType;
            barcodeIndex.ImageGeneratedAt = barcodeResult.GeneratedAt;

            // 保存条形码索引到数据库
            await _barcodeRepository.AddAsync(barcodeIndex);
            
            _logger.LogDebug("已为ExecutionTask {TaskId} 生成条形码索引和图片文件 {FilePath}", 
                task.Id, barcodeResult.FilePath ?? "内存中");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为ExecutionTask {TaskId} 生成条形码时发生错误", task.Id);
            // 条形码生成失败不应该影响任务的正常创建，所以这里只记录错误
        }
    }

    /// <summary>
    /// 根据用药途径获取任务分类
    /// </summary>
    private TaskCategory GetTaskCategoryFromUsageRoute(UsageRoute usageRoute)
    {
        return usageRoute switch
        {
            // 口服/外用类 - 立即执行
            UsageRoute.PO or UsageRoute.Topical => TaskCategory.Immediate,
            
            // 注射类 - 立即执行
            UsageRoute.IM or UsageRoute.SC or UsageRoute.IVP => TaskCategory.Immediate,
            
            // 持续输注类 - 持续执行
            UsageRoute.IVGTT or UsageRoute.Inhalation => TaskCategory.Duration,
            
            // 皮试类 - 结果等待
            UsageRoute.ST => TaskCategory.ResultPending,
            
            // 默认为立即执行
            _ => TaskCategory.Immediate
        };
    }

    /// <summary>
    /// 生成取药任务的数据载荷
    /// </summary>
    private string GenerateRetrieveMedicationDataPayload(MedicationOrder order, DateTime retrieveTime)
    {
        // 构建 Items 数组，用于 BarcodeMatchingService 的通用核对逻辑
        var items = new List<object>();
        var itemId = 1;

        if (order.Items != null && order.Items.Any())
        {
            foreach (var item in order.Items)
            {
                var drugName = item.Drug?.GenericName ?? 
                              item.Drug?.TradeName ?? 
                              $"药品({item.DrugId})";
                
                items.Add(new
                {
                    id = itemId++,
                    text = $"核对药品：{drugName} {item.Dosage}",
                    isChecked = false,
                    required = true,
                    drugId = item.DrugId // 关键字段：用于扫码匹配
                });
            }
        }

        var payload = new
        {
            TaskType = "RetrieveMedication",
            Title = "药房取药核对",
            Description = "请从药房取药并核对药品信息",
            IsChecklist = true,
            Items = items, // 符合通用协议的 Items 数组
            
            // 保留原有信息供参考
            OrderId = order.Id,
            PatientId = order.PatientId,
            RetrieveTime = retrieveTime,
            Medications = order.Items?.Select(item => new
            {
                DrugId = item.DrugId,
                DrugName = item.Drug?.GenericName,
                Dosage = item.Dosage,
                Note = item.Note
            }) ?? Enumerable.Empty<object>(),
            UsageRoute = order.UsageRoute,
            TotalItems = order.Items?.Count ?? 0
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    #endregion
}