using CareFlow.Application.Options;
using CareFlow.Application.Services.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareFlow.WebApi.BackgroundServices;

/// <summary>
/// 护理任务调度器 (BackgroundService)
/// 负责管理所有定时任务的调度
/// </summary>
public class NursingTaskScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly NursingScheduleOptions _options;
    private readonly ILogger<NursingTaskScheduler> _logger;
    private readonly TimeZoneInfo _chinaTimeZone;

    public NursingTaskScheduler(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<NursingScheduleOptions> options,
        ILogger<NursingTaskScheduler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
        _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 护理任务调度器启动");

        try
        {
            // 启动多个定时任务
            var tasks = new List<Task>
            {
                RunDailyTaskGeneratorAsync(stoppingToken),
                RunShiftHandoverAsync(stoppingToken),
                RunTaskReminderAsync(stoppingToken)
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 护理任务调度器异常停止");
            throw;
        }
    }

    /// <summary>
    /// 每日任务生成器（凌晨0点）
    /// </summary>
    private async Task RunDailyTaskGeneratorAsync(CancellationToken stoppingToken)
    {
        if (!_options.DailyTaskGeneration.Enabled)
        {
            _logger.LogInformation("ℹ️ 每日任务生成已禁用");
            return;
        }

        var triggerTime = TimeSpan.Parse(_options.DailyTaskGeneration.TriggerTime);
        _logger.LogInformation("⏰ 每日任务生成器已启动，触发时间: {Time}", triggerTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 计算下次执行时间
                var nowInChina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chinaTimeZone);
                var nextRun = CalculateNextRunTime(nowInChina, triggerTime);
                var delay = nextRun - nowInChina;

                _logger.LogDebug("⏳ 下次生成任务时间: {NextRun} (等待 {Delay})", 
                    nextRun.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                // 等待到下次执行时间
                await Task.Delay(delay, stoppingToken);

                // 执行任务生成
                _logger.LogInformation("🔔 触发每日任务生成");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dailyTaskGenerator = scope.ServiceProvider.GetRequiredService<DailyTaskGeneratorService>();
                    await dailyTaskGenerator.GenerateTodayTasksAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ℹ️ 每日任务生成器已停止");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 每日任务生成器执行失败，将在下个周期重试");
                // 发生错误后等待1分钟再重试
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 交班任务转移器（多个时间点）
    /// </summary>
    private async Task RunShiftHandoverAsync(CancellationToken stoppingToken)
    {
        if (!_options.ShiftHandover.Enabled)
        {
            _logger.LogInformation("ℹ️ 交班任务转移已禁用");
            return;
        }

        var shiftTimes = _options.ShiftHandover.ShiftTimes
            .Select(TimeSpan.Parse)
            .OrderBy(t => t)
            .ToList();

        _logger.LogInformation("⏰ 交班任务转移器已启动，交班时间: {Times}", 
            string.Join(", ", shiftTimes.Select(t => t.ToString(@"hh\:mm"))));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 计算下次交班时间
                var nowInChina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chinaTimeZone);
                var nextShiftTime = CalculateNextShiftTime(nowInChina, shiftTimes);
                var delay = nextShiftTime - nowInChina;

                _logger.LogDebug("⏳ 下次交班时间: {NextShift} (等待 {Delay})", 
                    nextShiftTime.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                // 等待到下次交班时间
                await Task.Delay(delay, stoppingToken);

                // 执行交班
                _logger.LogInformation("🔔 触发交班任务转移");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var shiftHandoverService = scope.ServiceProvider.GetRequiredService<ShiftHandoverService>();
                    await shiftHandoverService.TransferUnfinishedTasksAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ℹ️ 交班任务转移器已停止");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 交班任务转移执行失败，将在下个周期重试");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 逾期任务提醒器（高频检查）
    /// </summary>
    private async Task RunTaskReminderAsync(CancellationToken stoppingToken)
    {
        if (!_options.OverdueReminder.Enabled)
        {
            _logger.LogInformation("ℹ️ 逾期任务提醒已禁用");
            return;
        }

        var intervalMinutes = _options.OverdueReminder.IntervalMinutes;
        _logger.LogInformation("⏰ 逾期任务提醒器已启动，检查间隔: {Interval} 分钟", intervalMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);

                // 执行检查
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var taskReminderService = scope.ServiceProvider.GetRequiredService<TaskReminderService>();
                    await taskReminderService.CheckOverdueTasksAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ℹ️ 逾期任务提醒器已停止");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 逾期任务检查失败，将在下个周期重试");
            }
        }
    }

    /// <summary>
    /// 计算下次运行时间（单个时间点）
    /// </summary>
    private DateTime CalculateNextRunTime(DateTime nowInChina, TimeSpan targetTime)
    {
        var today = nowInChina.Date.Add(targetTime);

        // 如果今天的目标时间已过，返回明天的目标时间
        if (nowInChina >= today)
        {
            return today.AddDays(1);
        }

        return today;
    }

    /// <summary>
    /// 计算下次交班时间（多个时间点）
    /// </summary>
    private DateTime CalculateNextShiftTime(DateTime nowInChina, List<TimeSpan> shiftTimes)
    {
        var today = nowInChina.Date;

        // 查找今天剩余的交班时间
        foreach (var shiftTime in shiftTimes)
        {
            var shiftDateTime = today.Add(shiftTime);
            if (nowInChina < shiftDateTime)
            {
                return shiftDateTime;
            }
        }

        // 今天所有交班时间已过，返回明天的第一个交班时间
        return today.AddDays(1).Add(shiftTimes.First());
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 护理任务调度器正在停止...");
        return base.StopAsync(cancellationToken);
    }
}
