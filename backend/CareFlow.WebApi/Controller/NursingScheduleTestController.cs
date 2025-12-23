using CareFlow.Application.Services.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 护理任务调度测试控制器 (仅开发环境使用)
/// 用于手动触发定时任务，便于测试
/// </summary>
[ApiController]
[Route("api/nursing/schedule")]
public class NursingScheduleTestController : ControllerBase
{
    private readonly DailyTaskGeneratorService _dailyTaskGenerator;
    private readonly ShiftHandoverService _shiftHandoverService;
    private readonly TaskReminderService _taskReminderService;
    private readonly ILogger<NursingScheduleTestController> _logger;
    private readonly IWebHostEnvironment _environment;

    public NursingScheduleTestController(
        DailyTaskGeneratorService dailyTaskGenerator,
        ShiftHandoverService shiftHandoverService,
        TaskReminderService taskReminderService,
        ILogger<NursingScheduleTestController> logger,
        IWebHostEnvironment environment)
    {
        _dailyTaskGenerator = dailyTaskGenerator;
        _shiftHandoverService = shiftHandoverService;
        _taskReminderService = taskReminderService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 手动触发每日任务生成
    /// </summary>
    [HttpPost("trigger-daily-task")]
    public async Task<ActionResult> TriggerDailyTask()
    {
        // 仅在开发环境允许
        if (!_environment.IsDevelopment())
        {
            return BadRequest(new { message = "此接口仅在开发环境可用" });
        }

        try
        {
            _logger.LogInformation("📡 手动触发每日任务生成");
            await _dailyTaskGenerator.GenerateTodayTasksAsync();
            return Ok(new { message = "每日任务生成成功", timestamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动触发任务生成失败");
            return StatusCode(500, new { message = "任务生成失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 手动触发交班任务转移
    /// </summary>
    [HttpPost("trigger-shift-handover")]
    public async Task<ActionResult> TriggerShiftHandover()
    {
        if (!_environment.IsDevelopment())
        {
            return BadRequest(new { message = "此接口仅在开发环境可用" });
        }

        try
        {
            _logger.LogInformation("📡 手动触发交班任务转移");
            await _shiftHandoverService.TransferUnfinishedTasksAsync();
            return Ok(new { message = "交班任务转移成功", timestamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动触发交班转移失败");
            return StatusCode(500, new { message = "交班转移失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 手动触发逾期任务检查
    /// </summary>
    [HttpPost("trigger-reminder-check")]
    public async Task<ActionResult> TriggerReminderCheck()
    {
        if (!_environment.IsDevelopment())
        {
            return BadRequest(new { message = "此接口仅在开发环境可用" });
        }

        try
        {
            _logger.LogInformation("📡 手动触发逾期任务检查");
            await _taskReminderService.CheckOverdueTasksAsync();
            return Ok(new { message = "逾期任务检查成功", timestamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动触发逾期检查失败");
            return StatusCode(500, new { message = "逾期检查失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取调度器状态
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetSchedulerStatus()
    {
        return Ok(new
        {
            message = "护理任务调度器正在运行",
            environment = _environment.EnvironmentName,
            serverTime = DateTime.Now,
            serverTimeUtc = DateTime.UtcNow,
            chinaTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"))
        });
    }
}
