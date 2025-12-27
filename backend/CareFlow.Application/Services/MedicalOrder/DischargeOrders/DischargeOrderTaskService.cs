using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CareFlow.Application.Services.DischargeOrders;

/// <summary>
/// 出院医嘱任务拆分服务实现
/// </summary>
public class DischargeOrderTaskService : IDischargeOrderTaskService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<DischargeOrder, long> _dischargeOrderRepository;
    private readonly ILogger<DischargeOrderTaskService> _logger;

    public DischargeOrderTaskService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<DischargeOrder, long> dischargeOrderRepository,
        ILogger<DischargeOrderTaskService> logger)
    {
        _taskRepository = taskRepository;
        _dischargeOrderRepository = dischargeOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// 生成出院医嘱执行任务
    /// 任务1: 如果有带回药品，生成取药任务（Verification类型）
    /// 任务2: 生成出院确认任务（DischargeConfirmation类型）（无）
    /// </summary>
    public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(DischargeOrder order)
    {
        // 1. 输入参数验证
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "出院医嘱对象不能为空");
        }

        _logger.LogInformation("========== 开始为出院医嘱 {OrderId} 生成执行任务 ==========", order.Id);

        // 2. 验证医嘱是否真实存在于数据库中，并加载Items和Drug信息
        var existingOrder = await _dischargeOrderRepository.GetQueryable()
            .Include(o => o.Items)
                .ThenInclude(item => item.Drug)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        if (existingOrder == null)
        {
            var errorMsg = $"出院医嘱 {order.Id} 在数据库中不存在，无法生成执行任务";
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
            var errorMsg = $"出院医嘱 {order.Id} 状态为 {existingOrder.Status}，不允许生成执行任务";
            _logger.LogWarning(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // 4. 验证必要字段
        if (existingOrder.DischargeTime == default)
        {
            throw new InvalidOperationException($"出院医嘱 {order.Id} 缺少出院时间，无法生成任务");
        }

        if (existingOrder.DischargeType == 0)
        {
            throw new InvalidOperationException($"出院医嘱 {order.Id} 缺少出院类型，无法生成任务");
        }

        _logger.LogInformation("出院医嘱 {OrderId} 验证通过，开始生成执行任务", order.Id);

        // 5. 检查是否已存在未完成的任务
        var hasPendingTasks = await HasPendingTasksAsync(existingOrder.Id);
        if (hasPendingTasks)
        {
            _logger.LogWarning("出院医嘱 {OrderId} 已存在未完成的任务，建议先处理现有任务", order.Id);
            throw new InvalidOperationException($"出院医嘱 {order.Id} 已存在未完成的执行任务");
        }

        var tasks = new List<ExecutionTask>();

        try
        {
            // 6. 任务1: 如果有带回药品，生成取药任务
            if (existingOrder.Items != null && existingOrder.Items.Any())
            {
                _logger.LogInformation("出院医嘱包含 {Count} 项带回药品，生成取药任务", existingOrder.Items.Count);
                
                var retrieveTask = await GenerateRetrieveMedicationTaskAsync(existingOrder);
                tasks.Add(retrieveTask);
            }
            else
            {
                _logger.LogInformation("出院医嘱不包含带回药品，跳过取药任务");
            }

            // // 7. 任务2: 生成出院确认任务（必须）
            // var dischargeConfirmTask = await GenerateDischargeConfirmationTaskAsync(existingOrder);
            // tasks.Add(dischargeConfirmTask);

            _logger.LogInformation("✅ 成功为出院医嘱 {OrderId} 生成 {Count} 个执行任务", order.Id, tasks.Count);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成出院医嘱执行任务失败");
            throw;
        }
    }

    // ==================== 私有辅助方法 ====================

    /// <summary>
    /// 生成取药任务（带回药品）
    /// </summary>
    private async Task<ExecutionTask> GenerateRetrieveMedicationTaskAsync(DischargeOrder order)
    {
        _logger.LogInformation("--- 生成取药任务（带回药品）---");

        // 计划取药时间: 出院时间前2小时
        var plannedRetrieveTime = order.DischargeTime.AddHours(-2);

        // 构建任务数据负载
        var taskData = new Dictionary<string, object>
        {
            { "Title", "取药：出院带回药品" },
            { "OrderId", order.Id },
            { "OrderType", "DischargeOrder" },
            { "DischargeType", order.DischargeType.ToString() },
            { "DischargeTime", order.DischargeTime },
            { "IsDischargeOrder", true }, // 标记为出院医嘱任务
            { "MedicationItems", order.Items.Select(item => new
                {
                    item.DrugId,
                    DrugName = item.Drug?.GenericName ?? "未知药品",
                    item.Dosage,
                    item.Note
                }).ToList()
            }
        };

        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = TaskCategory.Verification, // 取药任务
            Status = ExecutionTaskStatus.Applying, // 初始状态为待申请
            PlannedStartTime = plannedRetrieveTime,
            DataPayload = JsonSerializer.Serialize(taskData, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }),
            CreateTime = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);

        _logger.LogInformation("✅ 取药任务创建成功，ID: {TaskId}, 计划时间: {PlannedTime}",
            task.Id, task.PlannedStartTime);

        return task;
    }

    /// <summary>
    /// 生成出院确认任务
    /// </summary>
    private async Task<ExecutionTask> GenerateDischargeConfirmationTaskAsync(DischargeOrder order)
    {
        _logger.LogInformation("--- 生成出院确认任务 ---");

        // 计划确认时间: 出院时间
        var plannedConfirmTime = order.DischargeTime;

        // 构建任务数据负载
        var taskData = new Dictionary<string, object>
        {
            { "Title", $"出院确认：{GetDischargeTypeDisplay(order.DischargeType)}" },
            { "OrderId", order.Id },
            { "OrderType", "DischargeOrder" },
            { "DischargeType", order.DischargeType.ToString() },
            { "DischargeTypeDisplay", GetDischargeTypeDisplay(order.DischargeType) },
            { "DischargeTime", order.DischargeTime },
            { "DischargeDiagnosis", order.DischargeDiagnosis ?? "" },
            { "DischargeInstructions", order.DischargeInstructions ?? "" },
            { "MedicationInstructions", order.MedicationInstructions ?? "" },
            { "RequiresFollowUp", order.RequiresFollowUp },
            { "FollowUpDate", (object?)order.FollowUpDate ?? "" },
            { "IsDischargeOrder", true } // 标记为出院医嘱任务
        };

        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = TaskCategory.DischargeConfirmation, // 出院确认任务
            Status = ExecutionTaskStatus.Pending, // 初始状态为待执行
            PlannedStartTime = plannedConfirmTime,
            DataPayload = JsonSerializer.Serialize(taskData, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }),
            CreateTime = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);

        _logger.LogInformation("✅ 出院确认任务创建成功，ID: {TaskId}, 计划时间: {PlannedTime}",
            task.Id, task.PlannedStartTime);

        return task;
    }

    /// <summary>
    /// 检查医嘱是否已有未完成的任务
    /// </summary>
    private async Task<bool> HasPendingTasksAsync(long orderId)
    {
        var incompletedStatuses = new[]
        {
            ExecutionTaskStatus.Pending,
            ExecutionTaskStatus.Applying,
            ExecutionTaskStatus.Applied,
            ExecutionTaskStatus.AppliedConfirmed,
            ExecutionTaskStatus.InProgress
        };

        var count = await _taskRepository.GetQueryable()
            .Where(t => t.MedicalOrderId == orderId && incompletedStatuses.Contains(t.Status))
            .CountAsync();

        return count > 0;
    }

    /// <summary>
    /// 获取出院类型显示名称
    /// </summary>
    private string GetDischargeTypeDisplay(DischargeType dischargeType)
    {
        return dischargeType switch
        {
            DischargeType.Cured => "治愈出院",
            DischargeType.Improved => "好转出院",
            DischargeType.Transfer => "转院",
            DischargeType.AutoDischarge => "自动出院",
            DischargeType.Death => "死亡",
            DischargeType.Other => "其他",
            _ => dischargeType.ToString()
        };
    }
}
