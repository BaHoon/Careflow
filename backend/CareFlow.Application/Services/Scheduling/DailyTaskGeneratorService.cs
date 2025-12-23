using CareFlow.Application.Interfaces;
using CareFlow.Application.Options;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareFlow.Application.Services.Scheduling;

/// <summary>
/// 每日任务生成服务
/// 负责在每天凌晨0点根据排班表生成当天的护理任务
/// </summary>
public class DailyTaskGeneratorService
{
    private readonly IRepository<Patient, string> _patientRepo;
    private readonly IRepository<NursingTask, long> _nursingTaskRepo;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly NursingScheduleOptions _options;
    private readonly ILogger<DailyTaskGeneratorService> _logger;
    private readonly TimeZoneInfo _chinaTimeZone;

    public DailyTaskGeneratorService(
        IRepository<Patient, string> patientRepo,
        IRepository<NursingTask, long> nursingTaskRepo,
        INurseAssignmentService nurseAssignmentService,
        IOptions<NursingScheduleOptions> options,
        ILogger<DailyTaskGeneratorService> logger)
    {
        _patientRepo = patientRepo;
        _nursingTaskRepo = nursingTaskRepo;
        _nurseAssignmentService = nurseAssignmentService;
        _options = options.Value;
        _logger = logger;
        _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
    }

    /// <summary>
    /// 生成今天的护理任务
    /// </summary>
    public async Task GenerateTodayTasksAsync()
    {
        try
        {
            // 获取中国时间的今天
            var nowInChina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chinaTimeZone);
            var today = nowInChina.Date;

            _logger.LogInformation("📅 开始生成 {Date} 的护理任务", today.ToString("yyyy-MM-dd"));

            // 1. 查询所有在院患者
            var patients = await _patientRepo.GetQueryable()
                .Where(p => p.Status != "Discharged")
                .ToListAsync();

            if (patients.Count == 0)
            {
                _logger.LogWarning("⚠️ 没有在院患者，跳过任务生成");
                return;
            }

            _logger.LogInformation("✅ 查询到 {Count} 个在院患者", patients.Count);

            // 2. 解析配置的时段
            var timeSlots = ParseTimeSlots(_options.DailyTaskGeneration.TaskTimeSlots);
            _logger.LogInformation("⏰ 配置的时段: {Slots}", string.Join(", ", timeSlots.Select(t => t.ToString(@"hh\:mm"))));

            // 3. 生成任务
            var tasksToCreate = new List<NursingTask>();
            var assignmentErrors = 0;

            foreach (var patient in patients)
            {
                foreach (var timeSlot in timeSlots)
                {
                    // 组合成完整的中国时间
                    var scheduledTimeInChina = today.Add(timeSlot);

                    // 转换为UTC时间存储到数据库
                    var scheduledTimeUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledTimeInChina, _chinaTimeZone);

                    // 检查是否已存在该任务（幂等性）
                    var exists = await _nursingTaskRepo.GetQueryable()
                        .AnyAsync(t => t.PatientId == patient.Id
                                    && t.ScheduledTime == scheduledTimeUtc
                                    && t.TaskType == "Routine");

                    if (exists)
                    {
                        _logger.LogDebug("⏭️ 任务已存在: PatientId={PatientId}, Time={Time}", 
                            patient.Id, scheduledTimeInChina);
                        continue;
                    }

                    // 使用 INurseAssignmentService 计算负责护士
                    var assignedNurseId = await _nurseAssignmentService
                        .CalculateResponsibleNurseAsync(patient.Id, scheduledTimeUtc);

                    if (assignedNurseId == null)
                    {
                        assignmentErrors++;
                        _logger.LogWarning("⚠️ 未找到负责护士: PatientId={PatientId}, Time={Time}", 
                            patient.Id, scheduledTimeInChina);
                    }

                    var task = new NursingTask
                    {
                        PatientId = patient.Id,
                        ScheduledTime = scheduledTimeUtc,
                        AssignedNurseId = assignedNurseId,
                        Status = assignedNurseId != null ? ExecutionTaskStatus.Pending : ExecutionTaskStatus.Applying,//TODO
                        TaskType = "Routine",
                        Description = $"常规护理 - {timeSlot.ToString(@"hh\:mm")}"
                    };

                    tasksToCreate.Add(task);
                }
            }

            // 4. 批量插入数据库
            if (tasksToCreate.Count > 0)
            {
                foreach (var task in tasksToCreate)
                {
                    await _nursingTaskRepo.AddAsync(task);
                }
                _logger.LogInformation("✅ 成功生成 {Count} 条护理任务", tasksToCreate.Count);

                if (assignmentErrors > 0)
                {
                    _logger.LogWarning("⚠️ 其中 {ErrorCount} 条任务未分配护士（排班缺失）", assignmentErrors);
                }

                // 统计信息
                var assignedCount = tasksToCreate.Count(t => t.AssignedNurseId != null);
                var unassignedCount = tasksToCreate.Count - assignedCount;
                _logger.LogInformation("📊 分配统计: 已分配={Assigned}, 未分配={Unassigned}", 
                    assignedCount, unassignedCount);
            }
            else
            {
                _logger.LogInformation("ℹ️ 所有任务已存在，无需重复生成");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成每日任务失败");
            throw;
        }
    }

    /// <summary>
    /// 解析时间字符串为 TimeSpan 列表
    /// </summary>
    private List<TimeSpan> ParseTimeSlots(List<string> timeSlotStrings)
    {
        var result = new List<TimeSpan>();
        foreach (var timeStr in timeSlotStrings)
        {
            if (TimeSpan.TryParse(timeStr, out var timeSpan))
            {
                result.Add(timeSpan);
            }
            else
            {
                _logger.LogWarning("⚠️ 无效的时段配置: {TimeStr}", timeStr);
            }
        }
        return result;
    }
}
