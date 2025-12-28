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
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> _medicalOrderRepo;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly NursingScheduleOptions _options;
    private readonly ILogger<DailyTaskGeneratorService> _logger;
    private readonly TimeZoneInfo _chinaTimeZone;

    public DailyTaskGeneratorService(
        IRepository<Patient, string> patientRepo,
        IRepository<NursingTask, long> nursingTaskRepo,
        IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> medicalOrderRepo,
        INurseAssignmentService nurseAssignmentService,
        IOptions<NursingScheduleOptions> options,
        ILogger<DailyTaskGeneratorService> logger)
    {
        _patientRepo = patientRepo;
        _nursingTaskRepo = nursingTaskRepo;
        _medicalOrderRepo = medicalOrderRepo;
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
                .Where(p => p.Status == PatientStatus.Hospitalized || p.Status == PatientStatus.PendingDischarge)
                .ToListAsync();

            if (patients.Count == 0)
            {
                _logger.LogWarning("⚠️ 没有在院患者，跳过任务生成");
                return;
            }

            _logger.LogInformation("✅ 查询到 {Count} 个在院患者", patients.Count);

            // 2. 生成任务（根据护理等级）
            var tasksToCreate = new List<NursingTask>();
            var assignmentErrors = 0;

            foreach (var patient in patients)
            {
                // 检查待出院患者是否今天出院
                if (patient.Status == PatientStatus.PendingDischarge)
                {
                    var shouldSkip = await ShouldSkipDischargePatientAsync(patient.Id, today);
                    if (shouldSkip)
                    {
                        _logger.LogInformation("⏭️ 患者 {PatientId} 今天出院，跳过生成护理任务", patient.Id);
                        continue;
                    }
                }

                // 根据护理等级获取时间点
                var timeSlots = GetTimeSlotsByGrade((NursingGrade)patient.NursingGrade);
                
                _logger.LogDebug("📋 患者 {PatientId} 护理等级 {Grade}，生成 {Count} 个时段", 
                    patient.Id, (NursingGrade)patient.NursingGrade, timeSlots.Count);

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
    /// 根据护理等级返回时间点
    /// </summary>
    private List<TimeSpan> GetTimeSlotsByGrade(NursingGrade grade)
    {
        return grade switch
        {
            // 三级护理: 每日1次 (14:00)
            NursingGrade.Grade3 => new List<TimeSpan> { 
                new(14, 0, 0) 
            },

            // 二级护理: 每日2次 (08:00, 16:00)
            NursingGrade.Grade2 => new List<TimeSpan> { 
                new(8, 0, 0), 
                new(16, 0, 0) 
            },

            // 一级护理: 每日3次 (08:00, 16:00, 20:00)
            NursingGrade.Grade1 => new List<TimeSpan> { 
                new(8, 0, 0), 
                new(16, 0, 0),
                new(20, 0, 0)
            },

            // 特级护理: 每2小时一次，24小时不间断，逢双数整点 (00:00, 02:00, 04:00, 06:00, 08:00, 10:00, 12:00, 14:00, 16:00, 18:00, 20:00, 22:00)
            NursingGrade.Special => new List<TimeSpan> { 
                new(0, 0, 0), new(2, 0, 0), new(4, 0, 0), new(6, 0, 0),
                new(8, 0, 0), new(10, 0, 0), new(12, 0, 0), new(14, 0, 0),
                new(16, 0, 0), new(18, 0, 0), new(20, 0, 0), new(22, 0, 0)
            },

            _ => new List<TimeSpan>() // 默认不生成
        };
    }

    /// <summary>
    /// 检查待出院患者是否在指定日期出院
    /// 如果出院时间为当前日期，则应跳过生成护理任务
    /// </summary>
    private async Task<bool> ShouldSkipDischargePatientAsync(string patientId, DateTime targetDate)
    {
        try
        {
            // 查询患者的出院医嘱
            var dischargeOrders = await _medicalOrderRepo.GetQueryable()
                .OfType<CareFlow.Core.Models.Medical.MedicalOrder>()
                .Where(o => o.PatientId == patientId && o.OrderType == "DischargeOrder")
                .ToListAsync();

            if (dischargeOrders.Count == 0)
            {
                _logger.LogWarning("⚠️ 患者 {PatientId} 状态为待出院但无出院医嘱", patientId);
                return false;
            }

            // 获取最新的出院医嘱（按创建时间倒序）
            var latestDischargeOrder = dischargeOrders
                .OrderByDescending(o => o.CreateTime)
                .FirstOrDefault() as CareFlow.Core.Models.Medical.DischargeOrder;

            if (latestDischargeOrder == null)
            {
                _logger.LogWarning("⚠️ 患者 {PatientId} 出院医嘱类型转换失败", patientId);
                return false;
            }

            // 比较出院时间是否为当前日期
            var dischargeTimeInChina = TimeZoneInfo.ConvertTimeFromUtc(latestDischargeOrder.DischargeTime, _chinaTimeZone);
            var dischargeDate = dischargeTimeInChina.Date;

            if (dischargeDate == targetDate)
            {
                _logger.LogInformation("🚪 患者 {PatientId} 出院时间为 {DischargeTime}，需要跳过任务生成", 
                    patientId, dischargeDate.ToString("yyyy-MM-dd"));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查患者 {PatientId} 是否需要跳过出院护理任务时失败", patientId);
            return false;
        }
    }
}
