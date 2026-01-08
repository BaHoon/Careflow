using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Enums;
using CareFlow.Core.Utils;
using System.Text.Json.Serialization;

namespace CareFlow.Application.Services.MedicalOrder.OperationOrders;

/// <summary>
/// 操作医嘱任务服务（参照药品医嘱实现）
/// </summary>
public interface IOperationOrderTaskService
{
    Task<List<ExecutionTask>> GenerateExecutionTasksAsync(OperationOrder order);
    Task<List<ExecutionTask>> RefreshExecutionTasksAsync(OperationOrder order);
    Task RollbackPendingTasksAsync(long orderId, string reason);
    Task CheckAndUpdateOrderStatusAsync(long orderId);
}

public class OperationOrderTaskService : IOperationOrderTaskService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<OperationOrder, long> _operationOrderRepository;
    private readonly IRepository<HospitalTimeSlot, int> _timeSlotRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly ILogger<OperationOrderTaskService> _logger;

    // 操作代码到操作名称的映射
    private static readonly Dictionary<string, string> OperationNameMap = new()
    {
        // 一、呼吸道管理类
        { "OP001", "持续低流量吸氧" },
        { "OP002", "雾化吸入治疗" },
        { "OP003", "经口/鼻吸痰" },
        { "OP004", "气管切开护理" },
        
        // 二、管路置入与维护类
        { "OP005", "留置胃管(鼻饲管置入)" },
        { "OP006", "胃肠减压护理" },
        { "OP007", "留置导尿术" },
        { "OP008", "更换引流袋/尿袋" },
        { "OP009", "膀胱冲洗" },
        { "OP010", "大量不保留灌肠" },
        
        // 三、静脉与标本采集类
        { "OP011", "快速血糖监测(末梢)" },
        { "OP012", "静脉留置针置管" },
        { "OP013", "静脉采血" },
        { "OP014", "动脉血气采集" },
        { "OP015", "静脉输血护理" },
        
        // 四、伤口与皮肤护理类
        { "OP016", "普通换药/敷料更换" },
        { "OP017", "造口护理" },
        { "OP018", "手术切口拆线" },
        
        // 五、仪器监测与治疗类
        { "OP019", "心电监护" },
        { "OP020", "微量泵/注射泵使用" },
        { "OP021", "气压治疗(预防血栓)" }
    };

    public OperationOrderTaskService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<OperationOrder, long> operationOrderRepository,
        IRepository<HospitalTimeSlot, int> timeSlotRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        ILogger<OperationOrderTaskService> logger)
    {
        _taskRepository = taskRepository;
        _operationOrderRepository = operationOrderRepository;
        _timeSlotRepository = timeSlotRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// 根据医嘱生成执行任务（参照药品医嘱实现）
    /// </summary>
    public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(OperationOrder order)
    {
        // 生成任务拆分逻辑
        // 1. 输入参数验证
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "医嘱对象不能为空");
        }

        _logger.LogInformation("开始为医嘱 {OrderId} 生成执行任务", order.Id);

        // 2. 验证医嘱是否真实存在于数据库中
        var existingOrder = await _operationOrderRepository.GetQueryable()
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        if (existingOrder == null)
        {
            var errorMsg = $"医嘱 {order.Id} 在数据库中不存在，无法生成执行任务";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // 3. 验证医嘱状态是否允许生成任务
        if (existingOrder.Status == OrderStatus.Cancelled || 
            existingOrder.Status == OrderStatus.Completed ||
            existingOrder.Status == OrderStatus.Stopped ||
            existingOrder.Status == OrderStatus.StoppingInProgress ||
            existingOrder.Status == OrderStatus.Draft ||
            existingOrder.Status == OrderStatus.Rejected)
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
            // 6. 根据时间策略生成任务
            switch (existingOrder.TimingStrategy?.ToUpper())
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
                    throw new ArgumentException($"不支持的时间策略: {existingOrder.TimingStrategy}，必须指定有效的时间策略（IMMEDIATE/SPECIFIC/CYCLIC/SLOTS）");
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
                        // 注意：责任护士分配和条形码生成由 OrderAcknowledgementService 在签收时统一处理
                        await _taskRepository.AddAsync(task);
                        savedTaskCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存执行任务时发生错误，任务计划时间: {PlannedTime}", 
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
        // 医嘱回滚（参照药品医嘱，更新状态而非物理删除）
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
        var existingOrder = await _operationOrderRepository.GetByIdAsync(orderId);
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
            // 注意：操作类医嘱没有取药任务，所以不需要检查Applying/Applied/AppliedConfirmed状态
            // 操作类医嘱的任务状态只有四种：Pending、InProgress、Completed、Stopped
            // 只检查Pending、InProgress状态，且ActualStartTime为null（未开始执行）
            var pendingTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == ExecutionTaskStatus.Pending || 
                 t.Status == ExecutionTaskStatus.InProgress) &&
                t.ActualStartTime == null); // 只回滚未开始执行的任务

            foreach (var task in pendingTasks)
            {
                // 更新状态为Stopped，不物理删除
                // 注意：条形码不删除，与药品类医嘱保持一致
                task.Status = ExecutionTaskStatus.Stopped;
                task.ExceptionReason = $"医嘱停止: {reason}";
                task.IsRolledBack = true;
                task.LastModifiedAt = DateTime.UtcNow;
                
                await _taskRepository.UpdateAsync(task);
            }

            _logger.LogInformation("已回滚医嘱 {OrderId} 的 {TaskCount} 个未执行任务", orderId, pendingTasks.Count());

            // 更新医嘱状态（不删除医嘱）
            if (existingOrder.Status != OrderStatus.Stopped && 
                existingOrder.Status != OrderStatus.StoppingInProgress)
            {
                // 保存原状态用于历史记录
                var originalStatus = existingOrder.Status;
                
                existingOrder.Status = OrderStatus.Stopped;
                existingOrder.StopReason = reason;
                existingOrder.StopOrderTime = DateTime.UtcNow;
                await _operationOrderRepository.UpdateAsync(existingOrder);
                
                // 添加医嘱状态变更历史记录
                var history = new MedicalOrderStatusHistory
                {
                    MedicalOrderId = existingOrder.Id,
                    FromStatus = originalStatus,
                    ToStatus = OrderStatus.Stopped,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = "System",
                    ChangedByType = "System",
                    Reason = $"回滚未执行任务并停止医嘱: {reason}"
                };
                await _statusHistoryRepository.AddAsync(history);
                
                _logger.LogInformation("医嘱 {OrderId} 状态已更新为 Stopped，并添加历史记录", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚医嘱 {OrderId} 的任务时发生错误", orderId);
            throw;
        }
    }

    public async Task<List<ExecutionTask>> RefreshExecutionTasksAsync(OperationOrder order)
    {
        // 医嘱更新后刷新任务（参照药品类医嘱实现）
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
        var existingOrder = await _operationOrderRepository.GetByIdAsync(order.Id);
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

            // 2. 重新生成任务并返回新任务列表（用于后续生成条形码）
            var newTasks = await GenerateExecutionTasksAsync(existingOrder);

            _logger.LogInformation("已刷新医嘱 {OrderId} 的执行任务，新生成 {TaskCount} 个任务", order.Id, newTasks?.Count ?? 0);
            
            return newTasks ?? new List<ExecutionTask>();
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
    private async Task<List<ExecutionTask>> GenerateImmediateTasks(OperationOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        var executionTime = DateTime.UtcNow;
        var tasks = new List<ExecutionTask>();
        
        var task = CreateOperationTask(order, executionTime, GetTaskCategoryFromOpId(order.OpId));
        tasks.Add(task);
        
        return tasks;
    }

    /// <summary>
    /// 生成指定时间执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateSpecificTasks(OperationOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (!order.StartTime.HasValue)
        {
            throw new ArgumentException("SPECIFIC策略必须指定执行时间（StartTime）");
        }

        var now = DateTime.UtcNow;
        var executionTime = order.StartTime.Value;
        var tasks = new List<ExecutionTask>();

        // 允许为过去的时间生成任务，但记录日志提醒
        if (executionTime <= now)
        {
            _logger.LogInformation("医嘱 {OrderId} 的执行时间 {StartTime} 早于当前时间，仍然生成任务（可能需要立即执行）", 
                order.Id, executionTime);
        }
        
        var task = CreateOperationTask(order, executionTime, GetTaskCategoryFromOpId(order.OpId));
        tasks.Add(task);
        
        return tasks;
    }

    /// <summary>
    /// 生成周期性执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateCyclicTasks(OperationOrder order)
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
        var now = DateTime.UtcNow;

        // 按时间间隔循环生成任务，直到超过结束时间
        // 为所有时间点生成任务（包括过去的），过去的任务会显示为逾期状态
        while (currentExecutionTime <= endTime)
        {
            // 对于长期医嘱，跳过早于医嘱开始时间的任务
            if (order.IsLongTerm && order.StartTime.HasValue && currentExecutionTime < order.StartTime.Value)
            {
                currentExecutionTime = currentExecutionTime.AddHours(intervalHours);
                continue;
            }
            
            var task = CreateOperationTask(order, currentExecutionTime, GetTaskCategoryFromOpId(order.OpId));
            tasks.Add(task);

            // 移动到下一次执行时间
            currentExecutionTime = currentExecutionTime.AddHours(intervalHours);
        }
        
        _logger.LogInformation("医嘱 {OrderId} CYCLIC策略生成 {TaskCount} 个任务（包括 {PastCount} 个过去时间点的任务）", 
            order.Id, tasks.Count, tasks.Count(t => t.PlannedStartTime < now));

        return tasks;
    }

    /// <summary>
    /// 生成时段执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateSlotsTasks(OperationOrder order)
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

        var now = DateTime.UtcNow;
        
        // 为所有时间点生成任务（包括过去的），过去的任务会显示为逾期状态
        for (var date = startDate; date <= endDate; date = date.AddDays(order.IntervalDays))
        {
            foreach (var slot in timeSlots)
            {
                var executionTime = date.Add(slot.DefaultTime);
                
                // 对于长期医嘱，跳过早于医嘱开始时间的任务
                if (order.IsLongTerm && order.StartTime.HasValue && executionTime < order.StartTime.Value)
                {
                    continue;
                }
                
                var task = CreateOperationTask(order, executionTime, GetTaskCategoryFromOpId(order.OpId), slot.SlotName);
                tasks.Add(task);
            }
        }
        
        _logger.LogInformation("医嘱 {OrderId} SLOTS策略生成 {TaskCount} 个任务（包括 {PastCount} 个过去时间点的任务）", 
            order.Id, tasks.Count, tasks.Count(t => t.PlannedStartTime < now));

        return tasks;
    }

    #endregion

    #region 验证方法

    /// <summary>
    /// 验证医嘱字段的完整性和有效性
    /// </summary>
    private async Task ValidateOrderFields(OperationOrder order)
    {
        var validationErrors = new List<string>();

        // 检查必要字段
        if (string.IsNullOrWhiteSpace(order.PatientId))
            validationErrors.Add("患者ID不能为空");

        if (string.IsNullOrWhiteSpace(order.DoctorId))
            validationErrors.Add("医生ID不能为空");

        if (string.IsNullOrWhiteSpace(order.OpId))
            validationErrors.Add("操作代码不能为空");

        if (string.IsNullOrWhiteSpace(order.OperationName))
            validationErrors.Add("操作名称不能为空");
        
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
        if (!string.IsNullOrWhiteSpace(order.TimingStrategy))
        {
            switch (order.TimingStrategy.ToUpper())
            {
                case "SPECIFIC":
                    if (!order.StartTime.HasValue)
                        validationErrors.Add("SPECIFIC策略必须指定执行时间（StartTime）");
                    // 移除未来时间限制，允许指定过去的时间
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
    /// 注意：操作类医嘱没有取药任务（Verification类别），所以不需要检查Applying/Applied/AppliedConfirmed状态
    /// 操作类医嘱的任务状态只有四种：Pending、InProgress、Completed、Stopped
    /// </summary>
    private async Task<bool> HasPendingTasksAsync(long orderId)
    {
        try
        {
            // 操作类医嘱的任务状态只有四种：Pending、InProgress、Completed、Stopped
            // 不包含取药相关的状态（Applying、Applied、AppliedConfirmed）
            // 也不包含Running状态（操作类医嘱使用InProgress表示执行中）
            var existingTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == ExecutionTaskStatus.Pending || 
                 t.Status == ExecutionTaskStatus.InProgress));

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

    #region 任务创建方法（从 Factory 合并）

    /// <summary>
    /// 创建单个操作任务（原 Factory 的功能）
    /// </summary>
    private ExecutionTask CreateOperationTask(
        OperationOrder order, 
        DateTime plannedTime, 
        TaskCategory category, 
        string? slotName = null)
    {
        // 确定 TaskType 字符串
        string taskType = category switch
        {
            TaskCategory.Duration => "DURATION_OPERATION",
            TaskCategory.ResultPending => "RESULT_PENDING_OPERATION",
            _ => "IMMEDIATE_OPERATION"
        };

        // 解析操作要求和准备物品（JSON）
        Dictionary<string, object>? operationRequirements = null;
        List<string>? preparationItems = null;
        
        if (!string.IsNullOrWhiteSpace(order.OperationRequirements))
        {
            try
            {
                operationRequirements = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(order.OperationRequirements);
            }
            catch
            {
                // 解析失败时忽略
            }
        }

        if (!string.IsNullOrWhiteSpace(order.PreparationItems))
        {
            try
            {
                preparationItems = System.Text.Json.JsonSerializer.Deserialize<List<string>>(order.PreparationItems);
            }
            catch
            {
                // 解析失败时忽略
            }
        }

        // 为ResultPending类任务生成结果模板
        string? resultPayloadTemplate = null;
        if (category == TaskCategory.ResultPending)
        {
            // 优先使用医嘱中的ResultTemplate，否则根据OpId生成默认模板
            if (!string.IsNullOrWhiteSpace(order.ResultTemplate))
            {
                resultPayloadTemplate = order.ResultTemplate;
            }
            else
            {
                resultPayloadTemplate = GenerateResultPayloadTemplate(order.OpId);
            }
        }

        // 构建完整的DataPayload
        // 注意：使用 string.IsNullOrWhiteSpace 而不是 ?? 运算符
        // 因为 OperationName 可能被初始化为空字符串，而不是 null
        var operationName = string.IsNullOrWhiteSpace(order.OperationName)
            ? GetOperationName(order.OpId)
            : order.OperationName;
        var dataPayload = new
        {
            TaskType = taskType,
            Title = $"操作：{operationName}",
            Description = BuildDescription(order, slotName),
            OpId = order.OpId,
            OperationName = operationName,
            OperationSite = order.OperationSite,
            Normal = order.Normal,
            
            // 时间策略信息
            TimingStrategy = order.TimingStrategy,
            SlotName = slotName,
            
            // 执行要求
            Requirements = operationRequirements,
            RequiresPreparation = order.RequiresPreparation,
            PreparationItems = preparationItems,
            
            // 任务配置
            ExpectedDurationMinutes = order.ExpectedDurationMinutes,
            RequiresResult = order.RequiresResult,
            ResultPayloadTemplate = resultPayloadTemplate,
            
            // 执行模式
            RequiresScan = true,
            IsImmediate = category == TaskCategory.Immediate,
            ExecutionMode = category == TaskCategory.Immediate ? "立即执行" 
                           : category == TaskCategory.Duration ? "持续执行"
                           : "等待结果",
            
            // 备注
            Remarks = order.Remarks
        };

        return new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = category,
            PlannedStartTime = plannedTime,
            Status = ExecutionTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ResultPayload = null, // 初始为null，ResultPending类任务在护士输入结果后才算任务结束
            DataPayload = System.Text.Json.JsonSerializer.Serialize(dataPayload, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            })
        };
    }

    /// <summary>
    /// 构建任务描述
    /// </summary>
    private string BuildDescription(OperationOrder order, string? slotName)
    {
        var parts = new List<string>();
        
        parts.Add($"操作代码：{order.OpId}");
        
        if (!string.IsNullOrWhiteSpace(order.TimingStrategy))
        {
            parts.Add($"时间策略：{order.TimingStrategy}");
        }
        
        if (!string.IsNullOrWhiteSpace(slotName))
        {
            parts.Add($"时段：{slotName}");
        }
        
        return string.Join("，", parts);
    }

    /// <summary>
    /// 为ResultPending类操作生成结构化结果数据模板
    /// </summary>
    private string? GenerateResultPayloadTemplate(string opId)
    {
        return opId switch
        {
            // ResultPending 类操作
            "OP011" => System.Text.Json.JsonSerializer.Serialize(new { Value = 0.0, Unit = "mmol/L", Note = "" }), // 快速血糖监测(末梢)
            
            // Duration 类但需要记录结果的操作
            "OP006" => System.Text.Json.JsonSerializer.Serialize(new { DrainageAmount = 0.0, Unit = "ml", Color = "", Note = "" }), // 胃肠减压护理
            "OP009" => System.Text.Json.JsonSerializer.Serialize(new { IrrigationAmount = 0.0, Unit = "ml", Color = "", Note = "" }), // 膀胱冲洗
            "OP015" => System.Text.Json.JsonSerializer.Serialize(new { TransfusionAmount = 0.0, Unit = "ml", Reaction = "", Note = "" }), // 静脉输血护理
            
            // Immediate 类但需要记录结果的操作
            "OP010" => System.Text.Json.JsonSerializer.Serialize(new { Result = "", Amount = "", Note = "" }), // 大量不保留灌肠
            
            _ => null
        };
    }

    /// <summary>
    /// 根据操作代码获取操作名称
    /// </summary>
    private string GetOperationName(string opId)
    {
        return OperationNameMap.ContainsKey(opId) 
            ? OperationNameMap[opId] 
            : $"操作 {opId}";
    }

    #endregion

    #region 辅助方法

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
    /// 根据操作代码获取任务类别
    /// </summary>
    private TaskCategory GetTaskCategoryFromOpId(string opId)
    {
        return opId switch
        {
            // Immediate 类：即刻执行，扫码即完成
            "OP003" => TaskCategory.Immediate,  // 经口/鼻吸痰
            "OP004" => TaskCategory.Immediate,  // 气管切开护理
            "OP005" => TaskCategory.Immediate,  // 留置胃管(鼻饲管置入)
            "OP007" => TaskCategory.Immediate,  // 留置导尿术
            "OP008" => TaskCategory.Immediate,  // 更换引流袋/尿袋
            "OP012" => TaskCategory.Immediate,  // 静脉留置针置管
            "OP013" => TaskCategory.Immediate,  // 静脉采血
            "OP014" => TaskCategory.Immediate,  // 动脉血气采集
            "OP016" => TaskCategory.Immediate,  // 普通换药/敷料更换
            "OP017" => TaskCategory.Immediate,  // 造口护理
            "OP018" => TaskCategory.Immediate,  // 手术切口拆线
            
            // Duration 类：持续执行，需要开始和结束时间
            "OP001" => TaskCategory.Duration,   // 持续低流量吸氧
            "OP002" => TaskCategory.Duration,   // 雾化吸入治疗
            "OP019" => TaskCategory.Duration,   // 心电监护
            "OP020" => TaskCategory.Duration,   // 微量泵/注射泵使用
            "OP021" => TaskCategory.Duration,   // 气压治疗(预防血栓)
            
            // ResultPending 类：需要等待结果，录入结果后完成
            "OP006" => TaskCategory.ResultPending, // 胃肠减压护理（需要记录引流量和颜色）
            "OP009" => TaskCategory.ResultPending, // 膀胱冲洗（需要记录冲洗量和颜色）
            "OP010" => TaskCategory.ResultPending, // 大量不保留灌肠（需要记录结果和量）
            "OP011" => TaskCategory.ResultPending, // 快速血糖监测(末梢)
            "OP015" => TaskCategory.ResultPending, // 静脉输血护理（需要记录输血量和反应）
            
            // 默认为立即执行
            _ => TaskCategory.Immediate
        };
    }


    /// <summary>
    /// 检查所有任务是否完成，如果完成则更新医嘱的EndTime
    /// </summary>
    public async Task CheckAndUpdateOrderStatusAsync(long orderId)
    {
        try
        {
            var order = await _operationOrderRepository.GetByIdAsync(orderId);
            if (order == null) return;

            // 查询该医嘱的所有任务
            var allTasks = await _taskRepository.ListAsync(t => t.MedicalOrderId == orderId);
            
            if (!allTasks.Any())
            {
                _logger.LogWarning("医嘱 {OrderId} 没有关联的任务", orderId);
                return;
            }

            // 检查是否所有任务都已完成（Completed或Stopped）
            // 操作类医嘱的任务状态只有四种：Pending、InProgress、Completed、Stopped
            // 只要不是Completed或Stopped，就认为是未完成的任务
            var incompleteTasks = allTasks.Where(t => 
                t.Status != ExecutionTaskStatus.Completed && 
                t.Status != ExecutionTaskStatus.Stopped).ToList();

            if (!incompleteTasks.Any() && !order.EndTime.HasValue)
            {
                // 保存原状态用于历史记录
                var originalStatus = order.Status;
                
                // 所有任务都已完成，更新EndTime
                order.EndTime = DateTime.UtcNow;
                order.Status = OrderStatus.Completed;
                await _operationOrderRepository.UpdateAsync(order);
                
                // 添加医嘱状态变更历史记录
                var history = new MedicalOrderStatusHistory
                {
                    MedicalOrderId = order.Id,
                    FromStatus = originalStatus,
                    ToStatus = OrderStatus.Completed,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = "System",
                    ChangedByType = "System",
                    Reason = "操作医嘱下所有任务已完成，系统自动完成医嘱"
                };
                await _statusHistoryRepository.AddAsync(history);
                
                _logger.LogInformation("医嘱 {OrderId} 的所有任务已完成，已更新EndTime并添加历史记录", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查并更新医嘱状态失败，OrderId: {OrderId}", orderId);
            // 不抛出异常，避免影响主流程
        }
    }

    #endregion
}

