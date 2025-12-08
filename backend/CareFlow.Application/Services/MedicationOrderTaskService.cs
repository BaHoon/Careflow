using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services;

public class MedicationOrderTaskService : IMedicationOrderTaskService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<HospitalTimeSlot, int> _timeSlotRepository;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<MedicationOrderTaskService> _logger;

    public MedicationOrderTaskService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<HospitalTimeSlot, int> timeSlotRepository,
        IRepository<BarcodeIndex, string> barcodeRepository,
        IBarcodeService barcodeService,
        ILogger<MedicationOrderTaskService> logger)
    {
        _taskRepository = taskRepository;
        _timeSlotRepository = timeSlotRepository;
        _barcodeRepository = barcodeRepository;
        _barcodeService = barcodeService;
        _logger = logger;
    }

    public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(MedicationOrder order)
    {
        _logger.LogInformation("开始为医嘱 {OrderId} 生成执行任务", order.Id);

        var tasks = new List<ExecutionTask>();

        try
        {
            switch (order.TimingStrategy.ToUpper())
            {
                case "IMMEDIATE":
                    tasks.AddRange(await GenerateImmediateTasks(order));
                    break;
                case "SPECIFIC":
                    tasks.AddRange(await GenerateSpecificTasks(order));
                    break;
                case "CYCLIC":
                    tasks.AddRange(await GenerateCyclicTasks(order));
                    break;
                case "SLOTS":
                    tasks.AddRange(await GenerateSlotsTasks(order));
                    break;
                default:
                    throw new ArgumentException($"不支持的时间策略: {order.TimingStrategy}");
            }

            // 批量保存任务到数据库
            if (tasks.Any())
            {
                foreach (var task in tasks)
                {
                    // 先保存任务以获得ID
                    await _taskRepository.AddAsync(task);
                    
                    // 为任务生成条形码索引
                    await GenerateBarcodeForTask(task);
                }
                _logger.LogInformation("已为医嘱 {OrderId} 生成 {TaskCount} 个执行任务", order.Id, tasks.Count);
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
        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            PlannedStartTime = DateTime.UtcNow,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        return new List<ExecutionTask> { task };
    }

    /// <summary>
    /// 生成指定时间执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateSpecificTasks(MedicationOrder order)
    {
        if (!order.SpecificExecutionTime.HasValue)
        {
            throw new ArgumentException("SPECIFIC策略必须指定具体执行时间");
        }

        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            PlannedStartTime = order.SpecificExecutionTime.Value,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        return new List<ExecutionTask> { task };
    }

    /// <summary>
    /// 生成周期性执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateCyclicTasks(MedicationOrder order)
    {
        var tasks = new List<ExecutionTask>();
        var startDate = (order.StartTime ?? DateTime.UtcNow).Date;
        var endDate = order.PlantEndTime.Date;

        // 根据FreqCode确定每日执行次数
        var dailyFreq = GetDailyFrequency(order.FreqCode);

        for (var date = startDate; date <= endDate; date = date.AddDays(order.IntervalDays))
        {
            for (int i = 0; i < dailyFreq; i++)
            {
                var executionTime = CalculateExecutionTime(date, i, dailyFreq);
                
                // 只生成未来时间的任务
                if (executionTime > DateTime.UtcNow)
                {
                    tasks.Add(new ExecutionTask
                    {
                        MedicalOrderId = order.Id,
                        PatientId = order.PatientId,
                        PlannedStartTime = executionTime,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        return tasks;
    }

    /// <summary>
    /// 生成时段执行的任务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateSlotsTasks(MedicationOrder order)
    {
        var tasks = new List<ExecutionTask>();
        
        // 解析时段位掩码
        var timeSlots = await ParseSlotsMask(order.SmartSlotsMask);
        
        if (!timeSlots.Any())
        {
            throw new ArgumentException("SmartSlotsMask未匹配到任何有效时段");
        }

        var startDate = (order.StartTime ?? DateTime.UtcNow).Date;
        var endDate = order.PlantEndTime.Date;

        for (var date = startDate; date <= endDate; date = date.AddDays(order.IntervalDays))
        {
            foreach (var slot in timeSlots)
            {
                var executionTime = date.Add(slot.DefaultTime);
                
                // 只生成未来时间的任务
                if (executionTime > DateTime.UtcNow)
                {
                    tasks.Add(new ExecutionTask
                    {
                        MedicalOrderId = order.Id,
                        PatientId = order.PatientId,
                        PlannedStartTime = executionTime,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        DataPayload = $"{{\"slotCode\":\"{slot.SlotCode}\",\"slotName\":\"{slot.SlotName}\"}}"
                    });
                }
            }
        }

        return tasks;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 根据频次代码获取每日执行次数
    /// </summary>
    private int GetDailyFrequency(string freqCode)
    {
        return freqCode.ToUpper() switch
        {
            "ONCE" or "QD" => 1,
            "BID" => 2,
            "TID" => 3,
            "QID" => 4,
            "Q6H" => 4,  // 每6小时
            "Q8H" => 3,  // 每8小时
            "Q12H" => 2, // 每12小时
            "PRN" => 1,  // 需要时，按1次处理
            "CONT" => 1, // 持续，按1次处理
            _ => 1
        };
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
                Id = $"ExecutionTask-{task.Id}", // 使用表名和ID作为唯一标识
                TableName = "ExecutionTask",
                RecordId = task.Id.ToString()
            };

            // 保存条形码索引到数据库
            await _barcodeRepository.AddAsync(barcodeIndex);
            
            // 生成条形码图片（可选，如果需要立即生成图片的话）
            // var barcodeBytes = _barcodeService.GenerateBarcode(barcodeIndex);
            // 这里可以选择保存到文件系统或其他地方
            
            _logger.LogDebug("已为ExecutionTask {TaskId} 生成条形码索引", task.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为ExecutionTask {TaskId} 生成条形码时发生错误", task.Id);
            // 条形码生成失败不应该影响任务的正常创建，所以这里只记录错误
        }
    }

    #endregion
}