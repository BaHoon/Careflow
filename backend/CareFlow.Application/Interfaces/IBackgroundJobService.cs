namespace CareFlow.Application.Interfaces;

/// <summary>
/// 后台任务调度服务接口
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// 延迟执行任务
    /// </summary>
    /// <param name="action">要执行的任务</param>
    /// <param name="delay">延迟时间</param>
    void ScheduleDelayed(Func<Task> action, TimeSpan delay);
}
