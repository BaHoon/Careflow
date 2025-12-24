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
    Task RefreshExecutionTasksAsync(OperationOrder order);
    Task RollbackPendingTasksAsync(long orderId, string reason);
    Task CheckAndUpdateOrderStatusAsync(long orderId);
}

public class OperationOrderTaskService : IOperationOrderTaskService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
    private readonly IRepository<OperationOrder, long> _operationOrderRepository;
    private readonly IRepository<HospitalTimeSlot, int> _timeSlotRepository;
    private readonly IBarcodeService _barcodeService;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<OperationOrderTaskService> _logger;

    // 操作代码到操作名称的映射（从原 Factory 合并）
    private static readonly Dictionary<string, string> OperationNameMap = new()
    {
        { "OP001", "更换引流袋" },
        { "OP002", "持续吸氧" },
        { "OP003", "血糖监测" },
        { "OP004", "更换敷料" },
        { "OP005", "导尿" },
        { "OP006", "鼻饲" },
        { "OP007", "雾化吸入" },
        { "OP008", "口腔护理" },
        { "OP009", "会阴护理" },
        { "OP010", "皮肤护理" },
        { "OP011", "持续心电监护" },
        { "OP012", "持续导尿" },
        { "OP013", "持续胃肠减压" },
        { "OP014", "持续静脉输液" },
        { "OP015", "翻身拍背" },
        { "OP016", "血压监测" },
        { "OP017", "体温监测" },
        { "OP018", "尿量监测" },
        { "OP019", "意识状态评估" },
        { "OP020", "疼痛评估" }
    };

    public OperationOrderTaskService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<BarcodeIndex, string> barcodeRepository,
        IRepository<OperationOrder, long> operationOrderRepository,
        IRepository<HospitalTimeSlot, int> timeSlotRepository,
        IBarcodeService barcodeService,
        INurseAssignmentService nurseAssignmentService,
        ILogger<OperationOrderTaskService> logger)
    {
        _taskRepository = taskRepository;
        _barcodeRepository = barcodeRepository;
        _operationOrderRepository = operationOrderRepository;
        _timeSlotRepository = timeSlotRepository;
        _barcodeService = barcodeService;
        _nurseAssignmentService = nurseAssignmentService;
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
                        await _taskRepository.AddAsync(task);
                        savedTaskCount++;

                        // 为任务分配责任护士
                        await AssignResponsibleNurseAsync(task, existingOrder.PatientId);

                        // 生成条形码索引
                        await GenerateBarcodeForTask(task);
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
            var pendingTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == ExecutionTaskStatus.Pending || 
                 t.Status == ExecutionTaskStatus.InProgress) && 
                t.ActualStartTime == null);

            foreach (var task in pendingTasks)
            {
                // 更新状态为Stopped，不物理删除
                task.Status = ExecutionTaskStatus.Stopped;
                task.ExceptionReason = $"医嘱停止: {reason}";
                task.IsRolledBack = true;
                task.LastModifiedAt = DateTime.UtcNow;
                
                await _taskRepository.UpdateAsync(task);

                // 删除条形码索引（如果需要）
                var barcodeId = $"Exec-{task.Id}";
                var barcode = await _barcodeRepository.GetByIdAsync(barcodeId);
                if (barcode != null)
                {
                    await _barcodeRepository.DeleteAsync(barcode);
                }
            }

            _logger.LogInformation("已回滚医嘱 {OrderId} 的 {TaskCount} 个未执行任务", orderId, pendingTasks.Count());

            // 更新医嘱状态（不删除医嘱）
            if (existingOrder.Status != OrderStatus.Stopped)
            {
                existingOrder.Status = OrderStatus.Stopped;
                existingOrder.StopReason = reason;
                existingOrder.StopOrderTime = DateTime.UtcNow;
                await _operationOrderRepository.UpdateAsync(existingOrder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚医嘱 {OrderId} 的任务时发生错误", orderId);
            throw;
        }
    }

    public async Task RefreshExecutionTasksAsync(OperationOrder order)
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

            // 2. 重新生成任务
            await GenerateExecutionTasksAsync(existingOrder);

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

        // 只生成未来时间的任务
        if (order.StartTime.Value <= DateTime.UtcNow)
        {
            _logger.LogWarning("医嘱 {OrderId} 的执行时间 {StartTime} 已过期，跳过任务生成", 
                order.Id, order.StartTime.Value);
            return new List<ExecutionTask>();
        }

        var executionTime = order.StartTime.Value;
        var tasks = new List<ExecutionTask>();
        
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

        // 按时间间隔循环生成任务，直到超过结束时间
        while (currentExecutionTime <= endTime)
        {
            // 只生成未来时间的任务
            if (currentExecutionTime > DateTime.UtcNow)
            {
                var task = CreateOperationTask(order, currentExecutionTime, GetTaskCategoryFromOpId(order.OpId));
                tasks.Add(task);
            }

            // 移动到下一次执行时间
            currentExecutionTime = currentExecutionTime.AddHours(intervalHours);
        }

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

        for (var date = startDate; date <= endDate; date = date.AddDays(order.IntervalDays))
        {
            foreach (var slot in timeSlots)
            {
                var executionTime = date.Add(slot.DefaultTime);
                
                // 只生成未来时间的任务
                if (executionTime > DateTime.UtcNow)
                {
                    var task = CreateOperationTask(order, executionTime, GetTaskCategoryFromOpId(order.OpId), slot.SlotName);
                    tasks.Add(task);
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
        var dataPayload = new
        {
            TaskType = taskType,
            Title = order.OperationName ?? GetOperationName(order.OpId),
            Description = BuildDescription(order, slotName),
            OpId = order.OpId,
            OperationName = order.OperationName ?? GetOperationName(order.OpId),
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
            "OP003" => System.Text.Json.JsonSerializer.Serialize(new { Value = 0.0, Unit = "mmol/L", Note = "" }), // 血糖监测
            "OP016" => System.Text.Json.JsonSerializer.Serialize(new { Systolic = 0, Diastolic = 0, Note = "" }), // 血压监测
            "OP017" => System.Text.Json.JsonSerializer.Serialize(new { Value = 0.0, Unit = "℃", Note = "" }), // 体温监测
            "OP018" => System.Text.Json.JsonSerializer.Serialize(new { Value = 0.0, Unit = "ml", Note = "" }), // 尿量监测
            "OP019" => System.Text.Json.JsonSerializer.Serialize(new { Level = "", Description = "", Note = "" }), // 意识状态评估
            "OP020" => System.Text.Json.JsonSerializer.Serialize(new { Score = 0, Level = "", Note = "" }), // 疼痛评估
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
            "OP001" => TaskCategory.Immediate,  // 更换引流袋
            "OP004" => TaskCategory.Immediate,  // 更换敷料
            "OP005" => TaskCategory.Immediate,  // 导尿
            "OP008" => TaskCategory.Immediate,  // 口腔护理
            "OP009" => TaskCategory.Immediate,  // 会阴护理
            "OP010" => TaskCategory.Immediate,  // 皮肤护理
            "OP015" => TaskCategory.Immediate,  // 翻身拍背
            
            // Duration 类：持续执行，需要开始和结束时间
            "OP002" => TaskCategory.Duration,   // 持续吸氧
            "OP006" => TaskCategory.Duration,   // 鼻饲
            "OP007" => TaskCategory.Duration,   // 雾化吸入
            "OP011" => TaskCategory.Duration,   // 持续心电监护
            "OP012" => TaskCategory.Duration,   // 持续导尿
            "OP013" => TaskCategory.Duration,   // 持续胃肠减压
            "OP014" => TaskCategory.Duration,   // 持续静脉输液
            
            // ResultPending 类：需要等待结果，录入结果后完成
            "OP003" => TaskCategory.ResultPending, // 血糖监测
            "OP016" => TaskCategory.ResultPending, // 血压监测
            "OP017" => TaskCategory.ResultPending, // 体温监测
            "OP018" => TaskCategory.ResultPending, // 尿量监测
            "OP019" => TaskCategory.ResultPending, // 意识状态评估
            "OP020" => TaskCategory.ResultPending, // 疼痛评估
            
            // 默认为立即执行
            _ => TaskCategory.Immediate
        };
    }

    /// <summary>
    /// 为任务分配责任护士
    /// </summary>
    private async Task AssignResponsibleNurseAsync(ExecutionTask task, string patientId)
    {
        try
        {
            var responsibleNurse = await _nurseAssignmentService
                .CalculateResponsibleNurseAsync(patientId, task.PlannedStartTime);

            if (responsibleNurse != null)
            {
                task.AssignedNurseId = responsibleNurse;
                await _taskRepository.UpdateAsync(task);
                _logger.LogInformation("任务 {TaskId} 分配计划责任护士 {NurseId}", task.Id, responsibleNurse);
            }
            else
            {
                _logger.LogWarning("任务 {TaskId} 计划时间 {Time} 无排班护士，计划责任护士留空",
                    task.Id, task.PlannedStartTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "为任务 {TaskId} 分配责任护士失败", task.Id);
        }
    }

    /// <summary>
    /// 为任务生成条形码索引
    /// </summary>
    private async Task GenerateBarcodeForTask(ExecutionTask task)
    {
        try
        {
            var barcodeIndex = new BarcodeIndex
            {
                Id = $"Exec-{task.Id}", 
                TableName = "ExecutionTasks",
                RecordId = task.Id.ToString()
            };
            await _barcodeRepository.AddAsync(barcodeIndex);
            
            _logger.LogDebug("为任务 {TaskId} 生成条形码索引成功", task.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为任务 {TaskId} 生成条形码失败", task.Id);
            // 条形码生成失败不影响任务创建，只记录日志
        }
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

            // 检查是否所有任务都已完成（Completed或Cancelled）
            var incompleteTasks = allTasks.Where(t => 
                t.Status != ExecutionTaskStatus.Completed && 
                t.Status != ExecutionTaskStatus.Cancelled && 
                t.Status != ExecutionTaskStatus.Incomplete).ToList();

            if (!incompleteTasks.Any() && !order.EndTime.HasValue)
            {
                // 所有任务都已完成，更新EndTime
                order.EndTime = DateTime.UtcNow;
                order.Status = OrderStatus.Completed;
                await _operationOrderRepository.UpdateAsync(order);
                _logger.LogInformation("医嘱 {OrderId} 的所有任务已完成，已更新EndTime", orderId);
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

