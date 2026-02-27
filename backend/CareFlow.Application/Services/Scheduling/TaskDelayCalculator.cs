using CareFlow.Application.Options;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using Microsoft.Extensions.Options;

namespace CareFlow.Application.Services.Scheduling;

/// <summary>
/// 任务延迟状态计算服务
/// 提供统一的延迟判断逻辑
/// </summary>
public class TaskDelayCalculator
{
    private readonly NursingScheduleOptions _options;

    public TaskDelayCalculator(IOptions<NursingScheduleOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 计算护理任务的延迟状态
    /// </summary>
    public TaskDelayStatus CalculateNursingTaskDelay(NursingTask task, DateTime nowUtc)
    {
        var delayMinutes = (int)(nowUtc - task.ScheduledTime).TotalMinutes;
        var allowedDelayMinutes = GetNursingTaskAllowedDelay(task.TaskType);
        var excessDelayMinutes = delayMinutes - allowedDelayMinutes;
        
        return new TaskDelayStatus
        {
            DelayMinutes = delayMinutes,
            AllowedDelayMinutes = allowedDelayMinutes,
            ExcessDelayMinutes = excessDelayMinutes,
            SeverityLevel = DetermineSeverityLevel(excessDelayMinutes)
        };
    }

    /// <summary>
    /// 计算执行任务的延迟状态
    /// </summary>
    public TaskDelayStatus CalculateExecutionTaskDelay(ExecutionTask task, DateTime nowUtc)
    {
        var delayMinutes = (int)(nowUtc - task.PlannedStartTime).TotalMinutes;
        var allowedDelayMinutes = GetExecutionTaskAllowedDelay(task);
        var excessDelayMinutes = delayMinutes - allowedDelayMinutes;
        
        return new TaskDelayStatus
        {
            DelayMinutes = delayMinutes,
            AllowedDelayMinutes = allowedDelayMinutes,
            ExcessDelayMinutes = excessDelayMinutes,
            SeverityLevel = DetermineSeverityLevel(excessDelayMinutes)
        };
    }

    /// <summary>
    /// 获取护理任务的允许延迟时间
    /// </summary>
    private int GetNursingTaskAllowedDelay(string taskType)
    {
        if (_options.OverdueReminder.NursingTaskTolerances.TryGetValue(taskType, out var allowed))
        {
            return allowed;
        }
        return 90; // 默认90分钟
    }

    /// <summary>
    /// 获取执行任务的允许延迟时间
    /// </summary>
    private int GetExecutionTaskAllowedDelay(ExecutionTask task)
    {
        // 空值保护：如果MedicalOrder未加载，返回默认值
        if (task.MedicalOrder == null)
        {
            return 30; // 默认30分钟
        }
        
        var orderType = task.MedicalOrder.OrderType;
        
        // 根据医嘱类型返回允许延迟时间
        return GetAllowedDelayFromConfig(orderType, 30);
    }

    /// <summary>
    /// 从配置中获取允许延迟时间
    /// </summary>
    private int GetAllowedDelayFromConfig(string key, int defaultValue)
    {
        if (_options.OverdueReminder.ExecutionTaskTolerances.TryGetValue(key, out var allowed))
        {
            return allowed;
        }
        return defaultValue;
    }

    /// <summary>
    /// 判断严重程度级别
    /// </summary>
    private string DetermineSeverityLevel(int excessDelayMinutes)
    {
        if (excessDelayMinutes <= 0)
        {
            return "Normal"; // 在允许范围内
        }
        
        var severeThreshold = _options.OverdueReminder.SevereDelayAfterToleranceMinutes;
        
        if (excessDelayMinutes >= severeThreshold)
        {
            return "Severe"; // 严重超时
        }
        
        return "Warning"; // 轻度超时
    }
}

/// <summary>
/// 任务延迟状态
/// </summary>
public class TaskDelayStatus
{
    /// <summary>
    /// 实际延迟时长（分钟）
    /// </summary>
    public int DelayMinutes { get; set; }
    
    /// <summary>
    /// 允许延迟时长（分钟）
    /// </summary>
    public int AllowedDelayMinutes { get; set; }
    
    /// <summary>
    /// 超出允许范围的时长（分钟）
    /// </summary>
    public int ExcessDelayMinutes { get; set; }
    
    /// <summary>
    /// 严重程度级别
    /// </summary>
    public string SeverityLevel { get; set; } = "Normal";
}
