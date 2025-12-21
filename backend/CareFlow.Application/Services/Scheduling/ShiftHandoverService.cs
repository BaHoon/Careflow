using CareFlow.Application.Interfaces;
using CareFlow.Application.Options;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareFlow.Application.Services.Scheduling;

/// <summary>
/// 交班任务转移服务
/// 负责在交班时将未完成的任务转移给新班次的护士
/// </summary>
public class ShiftHandoverService
{
    private readonly IRepository<NursingTask, long> _nursingTaskRepo;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly NursingScheduleOptions _options;
    private readonly ILogger<ShiftHandoverService> _logger;
    private readonly TimeZoneInfo _chinaTimeZone;

    public ShiftHandoverService(
        IRepository<NursingTask, long> nursingTaskRepo,
        INurseAssignmentService nurseAssignmentService,
        IOptions<NursingScheduleOptions> options,
        ILogger<ShiftHandoverService> logger)
    {
        _nursingTaskRepo = nursingTaskRepo;
        _nurseAssignmentService = nurseAssignmentService;
        _options = options.Value;
        _logger = logger;
        _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
    }

    /// <summary>
    /// 执行交班任务转移
    /// </summary>
    public async Task TransferUnfinishedTasksAsync()
    {
        try
        {
            var nowInChina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chinaTimeZone);
            _logger.LogInformation("🔄 开始执行交班任务转移 @ {Time}", nowInChina.ToString("HH:mm"));

            // 1. 查询所有未完成的任务
            var unfinishedTasks = await _nursingTaskRepo.GetQueryable()
                .Where(t => t.Status == "Pending")
                .ToListAsync();

            if (unfinishedTasks.Count == 0)
            {
                _logger.LogInformation("✅ 没有需要转移的未完成任务");
                return;
            }

            _logger.LogInformation("📋 查询到 {Count} 条未完成任务", unfinishedTasks.Count);

            var transferredCount = 0;
            var failedCount = 0;
            var unchangedCount = 0;

            // 2. 为每个任务重新计算负责护士
            foreach (var task in unfinishedTasks)
            {
                var oldNurseId = task.AssignedNurseId;

                // 使用当前时间重新计算负责护士
                var newNurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(task.PatientId, DateTime.UtcNow);

                if (newNurseId == null)
                {
                    failedCount++;
                    _logger.LogWarning("⚠️ 无法找到新班次护士: TaskId={TaskId}, PatientId={PatientId}", 
                        task.Id, task.PatientId);
                    
                    // 设置为未分配状态
                    task.AssignedNurseId = null;
                    task.Status = "Unassigned";
                    continue;
                }

                // 如果护士没有变化，跳过
                if (newNurseId == oldNurseId)
                {
                    unchangedCount++;
                    continue;
                }

                // 更新护士分配
                task.AssignedNurseId = newNurseId;
                
                // 确保状态正确
                if (task.Status == "Unassigned")
                {
                    task.Status = "Pending";
                }

                transferredCount++;
                _logger.LogDebug("🔀 任务转移: TaskId={TaskId}, {OldNurse} → {NewNurse}", 
                    task.Id, oldNurseId ?? "未分配", newNurseId);
            }

            // 3. 批量保存更改
            if (transferredCount > 0 || failedCount > 0)
            {
                foreach (var task in unfinishedTasks.Where(t => t.AssignedNurseId != null || t.Status == "Unassigned"))
                {
                    await _nursingTaskRepo.UpdateAsync(task);
                }
                _logger.LogInformation("✅ 交班完成: 转移={Transferred}, 未变化={Unchanged}, 失败={Failed}", 
                    transferredCount, unchangedCount, failedCount);
            }
            else
            {
                _logger.LogInformation("✅ 所有任务护士未变化，无需转移");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 交班任务转移失败");
            throw;
        }
    }
}
