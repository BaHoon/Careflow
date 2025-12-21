using CareFlow.Application.Options;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareFlow.Application.Services.Scheduling;

/// <summary>
/// 逾期任务提醒服务
/// 负责检查逾期未完成的任务并进行相应处理
/// 使用"容忍时间窗口"策略，避免无意义的提醒
/// </summary>
public class TaskReminderService
{
    private readonly IRepository<NursingTask, long> _nursingTaskRepo;
    private readonly IRepository<ExecutionTask, long> _executionTaskRepo;
    private readonly NursingScheduleOptions _options;
    private readonly ILogger<TaskReminderService> _logger;
    private readonly TimeZoneInfo _chinaTimeZone;

    public TaskReminderService(
        IRepository<NursingTask, long> nursingTaskRepo,
        IRepository<ExecutionTask, long> executionTaskRepo,
        IOptions<NursingScheduleOptions> options,
        ILogger<TaskReminderService> logger)
    {
        _nursingTaskRepo = nursingTaskRepo;
        _executionTaskRepo = executionTaskRepo;
        _options = options.Value;
        _logger = logger;
        _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
    }

    /// <summary>
    /// 检查逾期任务（包括 NursingTask 和 ExecutionTask）
    /// </summary>
    public async Task CheckOverdueTasksAsync()
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            var nowInChina = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _chinaTimeZone);

            _logger.LogDebug("🔍 检查逾期任务 @ {Time}", nowInChina.ToString("yyyy-MM-dd HH:mm"));

            // 分别检查两种任务
            await CheckOverdueNursingTasksAsync(nowUtc, nowInChina);
            await CheckOverdueExecutionTasksAsync(nowUtc, nowInChina);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查逾期任务失败");
            throw;
        }
    }

    /// <summary>
    /// 检查逾期的护理任务
    /// </summary>
    private async Task CheckOverdueNursingTasksAsync(DateTime nowUtc, DateTime nowInChina)
    {
        var overdueTasks = await _nursingTaskRepo.GetQueryable()
            .Where(t => t.Status == "Pending" && t.ScheduledTime < nowUtc)
            .Include(t => t.Patient)
            .Include(t => t.AssignedNurse)
            .ToListAsync();

        if (overdueTasks.Count == 0)
        {
            _logger.LogDebug("✅ 没有逾期的护理任务");
            return;
        }

        var warningCount = 0;
        var severeCount = 0;

        foreach (var task in overdueTasks)
        {
            // 获取该任务类型的容忍时间
            var toleranceMinutes = GetNursingTaskTolerance(task.TaskType);
            var delayMinutes = (int)(nowUtc - task.ScheduledTime).TotalMinutes;

            // 在容忍期内 → 不提醒
            if (delayMinutes <= toleranceMinutes)
            {
                continue;
            }

            var scheduledTimeInChina = TimeZoneInfo.ConvertTimeFromUtc(task.ScheduledTime, _chinaTimeZone);
            var overToleranceMinutes = delayMinutes - toleranceMinutes;
            var severeThreshold = _options.OverdueReminder.SevereDelayAfterToleranceMinutes;

            if (overToleranceMinutes < severeThreshold)
            {
                // 超过容忍期，但未达严重级别 → 警告
                warningCount++;
                _logger.LogWarning("⚠️ 护理任务轻度逾期: TaskId={TaskId}, Type={Type}, Patient={Patient}, " +
                    "Scheduled={Time}, 延迟={Delay}分钟 (容忍={Tolerance}分钟)", 
                    task.Id, task.TaskType, task.Patient?.Name, 
                    scheduledTimeInChina.ToString("HH:mm"), delayMinutes, toleranceMinutes);
            }
            else
            {
                // 严重逾期 → 告警
                severeCount++;
                _logger.LogError("🚨 护理任务严重逾期: TaskId={TaskId}, Type={Type}, Patient={Patient}, " +
                    "Nurse={Nurse}, Scheduled={Time}, 延迟={Delay}分钟 (容忍={Tolerance}分钟)", 
                    task.Id, task.TaskType, task.Patient?.Name, task.AssignedNurse?.Name ?? "未分配",
                    scheduledTimeInChina.ToString("HH:mm"), delayMinutes, toleranceMinutes);
            }
        }

        if (warningCount > 0 || severeCount > 0)
        {
            _logger.LogInformation("📊 护理任务逾期统计: 警告={Warning}, 严重={Severe}", warningCount, severeCount);
        }
    }

    /// <summary>
    /// 检查逾期的执行任务
    /// </summary>
    private async Task CheckOverdueExecutionTasksAsync(DateTime nowUtc, DateTime nowInChina)
    {
        var overdueTasks = await _executionTaskRepo.GetQueryable()
            .Where(t => t.Status == "Pending" && t.PlannedStartTime < nowUtc)
            .Include(t => t.Patient)
            .Include(t => t.MedicalOrder)
            .ToListAsync();

        if (overdueTasks.Count == 0)
        {
            _logger.LogDebug("✅ 没有逾期的执行任务");
            return;
        }

        var warningCount = 0;
        var severeCount = 0;

        foreach (var task in overdueTasks)
        {
            // 获取该任务类型的容忍时间
            var toleranceMinutes = GetExecutionTaskTolerance(task);
            var delayMinutes = (int)(nowUtc - task.PlannedStartTime).TotalMinutes;

            // 在容忍期内 → 不提醒
            if (delayMinutes <= toleranceMinutes)
            {
                continue;
            }

            var plannedTimeInChina = TimeZoneInfo.ConvertTimeFromUtc(task.PlannedStartTime, _chinaTimeZone);
            var overToleranceMinutes = delayMinutes - toleranceMinutes;
            var severeThreshold = _options.OverdueReminder.SevereDelayAfterToleranceMinutes;

            if (overToleranceMinutes < severeThreshold)
            {
                // 超过容忍期，但未达严重级别 → 警告
                warningCount++;
                _logger.LogWarning("⚠️ 执行任务轻度逾期: TaskId={TaskId}, OrderType={OrderType}, Patient={Patient}, " +
                    "Planned={Time}, 延迟={Delay}分钟 (容忍={Tolerance}分钟)", 
                    task.Id, task.MedicalOrder.OrderType, task.Patient?.Name,
                    plannedTimeInChina.ToString("HH:mm"), delayMinutes, toleranceMinutes);
            }
            else
            {
                // 严重逾期 → 告警
                severeCount++;
                _logger.LogError("🚨 执行任务严重逾期: TaskId={TaskId}, OrderType={OrderType}, Patient={Patient}, " +
                    "Planned={Time}, 延迟={Delay}分钟 (容忍={Tolerance}分钟)", 
                    task.Id, task.MedicalOrder.OrderType, task.Patient?.Name,
                    plannedTimeInChina.ToString("HH:mm"), delayMinutes, toleranceMinutes);
            }
        }

        if (warningCount > 0 || severeCount > 0)
        {
            _logger.LogInformation("📊 执行任务逾期统计: 警告={Warning}, 严重={Severe}", warningCount, severeCount);
        }
    }

    /// <summary>
    /// 获取护理任务的容忍时间（分钟）
    /// </summary>
    private int GetNursingTaskTolerance(string taskType)
    {
        if (_options.OverdueReminder.NursingTaskTolerances.TryGetValue(taskType, out var tolerance))
        {
            return tolerance;
        }

        // 默认：常规任务容忍90分钟
        _logger.LogWarning("⚠️ 未配置的护理任务类型: {TaskType}，使用默认容忍时间90分钟", taskType);
        return 90;
    }

    /// <summary>
    /// 获取执行任务的容忍时间（分钟）
    /// 根据关联的医嘱类型动态判断
    /// </summary>
    private int GetExecutionTaskTolerance(ExecutionTask task)
    {
        var orderType = task.MedicalOrder.OrderType;
        
        // 特殊处理：药品医嘱根据 TimingStrategy 细分
        if (orderType == "MedicationOrder")
        {
            var medicationOrder = task.MedicalOrder as CareFlow.Core.Models.Medical.MedicationOrder;
            if (medicationOrder?.TimingStrategy == "IMMEDIATE")
            {
                return GetToleranceFromConfig("MedicationOrder_IMMEDIATE", 15);
            }
            return GetToleranceFromConfig("MedicationOrder_Default", 30);
        }

        // 其他医嘱类型
        return GetToleranceFromConfig(orderType, 30);
    }

    /// <summary>
    /// 从配置中获取容忍时间
    /// </summary>
    private int GetToleranceFromConfig(string key, int defaultValue)
    {
        if (_options.OverdueReminder.ExecutionTaskTolerances.TryGetValue(key, out var tolerance))
        {
            return tolerance;
        }

        _logger.LogDebug("ℹ️ 未配置的执行任务类型: {Key}，使用默认容忍时间{Default}分钟", key, defaultValue);
        return defaultValue;
    }
}
