using CareFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services.OrderApplication;

/// <summary>
/// 简单的后台任务调度服务
/// 使用Task.Delay实现延迟执行
/// 企业级应用建议使用Hangfire或Quartz.NET
/// </summary>
public class SimpleBackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<SimpleBackgroundJobService> _logger;

    public SimpleBackgroundJobService(ILogger<SimpleBackgroundJobService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 延迟执行任务
    /// </summary>
    public void ScheduleDelayed(Func<Task> action, TimeSpan delay)
    {
        _logger.LogInformation("⏰ 安排延迟任务，延迟时间: {Delay}", delay);
        
        // 使用Task.Run启动后台任务，不阻塞当前线程
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay);
                _logger.LogInformation("🚀 开始执行延迟任务");
                await action();
                _logger.LogInformation("✅ 延迟任务执行完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 延迟任务执行失败");
            }
        });
    }
}
