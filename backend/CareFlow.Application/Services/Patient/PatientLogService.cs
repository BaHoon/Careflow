using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Patient;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Application.Services.Patient;

/// <summary>
/// 患者日志服务实现
/// </summary>
public class PatientLogService : IPatientLogService
{
    private readonly ICareFlowDbContext _context;

    public PatientLogService(ICareFlowDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取患者日志数据
    /// </summary>
    public async Task<PatientLogResponseDto> GetPatientLogAsync(PatientLogQueryDto query)
    {
        // 1. 获取患者基本信息
        var patient = await _context.Set<Core.Models.Organization.Patient>()
            .Include(p => p.Bed)
            .FirstOrDefaultAsync(p => p.Id == query.PatientId);
            
        if (patient == null)
        {
            throw new Exception($"未找到ID为 {query.PatientId} 的患者");
        }

        var response = new PatientLogResponseDto
        {
            Patient = new PatientBasicInfoDto
            {
                PatientId = patient.Id,
                PatientName = patient.Name,
                BedId = patient.BedId,
                Gender = patient.Gender,
                Age = patient.Age,
                NursingGrade = patient.NursingGrade.ToString()
            },
            DailyLogs = new List<DailyLogDto>()
        };

        // 2. 串行查询三大数据源
        // 🔧 修复DbContext并发问题：EF Core的DbContext不是线程安全的，必须串行执行
        // 原因：多个异步查询并行执行时会共用同一个_context实例，导致并发冲突
        // 解决方案：改为await顺序执行，虽然稍慢但避免了并发问题
        var executionRecords = query.ContentTypes.Contains("MedicalOrders")
            ? await GetExecutionRecordsAsync(query.PatientId, query.StartDate, query.EndDate)
            : new List<ExecutionTask>();

        var vitalSignsRecords = query.ContentTypes.Contains("NursingRecords")
            ? await GetVitalSignsRecordsAsync(query.PatientId, query.StartDate, query.EndDate)
            : new List<VitalSignsRecord>();

        var inspectionReports = query.ContentTypes.Contains("ExamReports")
            ? await GetInspectionReportsAsync(query.PatientId, query.StartDate, query.EndDate)
            : new List<InspectionReport>();

        // 3. 按日期分组处理
        var allDates = new HashSet<string>();
        
        // 收集所有日期
        foreach (var task in executionRecords.Where(t => t.ActualStartTime.HasValue))
        {
            allDates.Add(task.ActualStartTime!.Value.ToString("yyyy-MM-dd"));
        }
        foreach (var record in vitalSignsRecords)
        {
            allDates.Add(record.RecordTime.ToString("yyyy-MM-dd"));
        }
        foreach (var report in inspectionReports)
        {
            allDates.Add(report.ReportTime.ToString("yyyy-MM-dd"));
        }

        // 4. 为每一天构建日志数据
        foreach (var date in allDates.OrderBy(d => d))
        {
            var dailyLog = new DailyLogDto { Date = date };

            // 医嘱执行汇总
            if (query.ContentTypes.Contains("MedicalOrders"))
            {
                var dayTasks = executionRecords
                    .Where(t => t.ActualStartTime.HasValue && 
                                t.ActualStartTime.Value.ToString("yyyy-MM-dd") == date)
                    .ToList();
                
                if (dayTasks.Any())
                {
                    dailyLog.MedicalOrdersSummary = BuildMedicalOrdersSummary(dayTasks);
                }
            }

            // 护理记录汇总
            if (query.ContentTypes.Contains("NursingRecords"))
            {
                var dayRecords = vitalSignsRecords
                    .Where(r => r.RecordTime.ToString("yyyy-MM-dd") == date)
                    .ToList();
                
                if (dayRecords.Any())
                {
                    dailyLog.NursingRecordsSummary = BuildNursingRecordsSummary(dayRecords);
                }
            }

            // 检查报告汇总
            if (query.ContentTypes.Contains("ExamReports"))
            {
                var dayReports = inspectionReports
                    .Where(r => r.ReportTime.ToString("yyyy-MM-dd") == date)
                    .ToList();
                
                if (dayReports.Any())
                {
                    dailyLog.ExamReportsSummary = BuildExamReportsSummary(dayReports);
                }
            }

            response.DailyLogs.Add(dailyLog);
        }

        return response;
    }

    /// <summary>
    /// 查询医嘱执行记录 (基于ActualStartTime)
    /// </summary>
    private async Task<List<ExecutionTask>> GetExecutionRecordsAsync(
        string patientId, 
        DateTime startDate, 
        DateTime endDate)
    {
        // 🔧 修复PostgreSQL时区问题：将DateTime转换为UTC
        // 前端传递的日期字符串("2025-12-26")会被解析为Kind=Unspecified
        // 需要使用SpecifyKind明确指定为UTC，避免PostgreSQL报错
        var startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        
        return await _context.Set<ExecutionTask>()
            .Where(et => et.PatientId == patientId
                && et.ActualStartTime >= startDateUtc
                && et.ActualStartTime <= endDateUtc
                && et.ActualStartTime != null) // 必须已执行
            .Include(et => et.MedicalOrder) // 联表查询医嘱详情
                .ThenInclude(mo => (mo as MedicationOrder)!.Items) // 药品医嘱的药品项列表
                    .ThenInclude(item => item.Drug) // 药品详情
            .Include(et => et.Executor) // 执行护士
            .Include(et => et.AssignedNurse) // 负责护士
            .OrderBy(et => et.ActualStartTime)
            .ToListAsync();
    }

    /// <summary>
    /// 查询护理记录 (基于RecordTime)
    /// </summary>
    private async Task<List<VitalSignsRecord>> GetVitalSignsRecordsAsync(
        string patientId, 
        DateTime startDate, 
        DateTime endDate)
    {
        // 🔧 修复PostgreSQL时区问题：将DateTime转换为UTC
        var startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        
        return await _context.Set<VitalSignsRecord>()
            .Where(vr => vr.PatientId == patientId
                && vr.RecordTime >= startDateUtc
                && vr.RecordTime <= endDateUtc)
            .Include(vr => vr.RecorderNurse)
            .OrderBy(vr => vr.RecordTime)
            .ToListAsync();
    }

    /// <summary>
    /// 查询检查报告 (基于ReportTime)
    /// </summary>
    private async Task<List<InspectionReport>> GetInspectionReportsAsync(
        string patientId, 
        DateTime startDate, 
        DateTime endDate)
    {
        // 🔧 修复PostgreSQL时区问题：将DateTime转换为UTC
        var startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        
        return await _context.Set<InspectionReport>()
            .Where(ir => ir.PatientId == patientId
                && ir.ReportTime >= startDateUtc
                && ir.ReportTime <= endDateUtc)
            .Include(ir => ir.InspectionOrder) // 包含检查医嘱
            .Include(ir => ir.Reviewer) // 包含审核医生
            .OrderBy(ir => ir.ReportTime)
            .ToListAsync();
    }

    /// <summary>
    /// 构建医嘱执行汇总
    /// </summary>
    private MedicalOrdersSummaryDto BuildMedicalOrdersSummary(List<ExecutionTask> tasks)
    {
        // 按医嘱ID分组
        var groupedByOrder = tasks.GroupBy(t => t.MedicalOrderId);
        
        var records = new List<ExecutionRecordDto>();

        foreach (var group in groupedByOrder)
        {
            var firstTask = group.First();
            var order = firstTask.MedicalOrder;
            
            var record = new ExecutionRecordDto
            {
                OrderId = order.Id,
                OrderType = order.OrderType,
                OrderContent = GetOrderContent(order),
                Specification = GetOrderSpecification(order),
                IsLongTerm = GetOrderIsLongTerm(order),
                Summary = GetOrderSummary(order),
                PlannedEndTime = GetOrderPlannedEndTime(order),
                DischargeTime = GetOrderDischargeTime(order),
                Tasks = group.Select(t => new ExecutionTaskSummaryDto
                {
                    Id = t.Id,
                    ActualStartTime = t.ActualStartTime,
                    ActualEndTime = t.ActualEndTime,
                    ExecutorName = t.Executor?.Name,
                    AssignedNurseName = t.AssignedNurse?.Name,
                    DataPayload = t.DataPayload,
                    Category = t.Category,
                    Status = t.Status,
                    ResultPayload = t.ResultPayload, // 执行结果
                    ExecutionRemarks = t.ExecutionRemarks // 执行备注
                }).ToList()
            };

            records.Add(record);
        }

        return new MedicalOrdersSummaryDto
        {
            TotalCount = records.Count,
            Records = records
        };
    }

    /// <summary>
    /// 构建护理记录汇总
    /// </summary>
    private NursingRecordsSummaryDto BuildNursingRecordsSummary(List<VitalSignsRecord> records)
    {
        var summary = new NursingRecordsSummaryDto
        {
            TotalCount = records.Count,
            HasAbnormal = false,
            AbnormalDescriptions = new List<string>(),
            Records = new List<VitalSignRecordDto>()
        };

        foreach (var record in records)
        {
            var abnormalItems = CheckVitalSignsAbnormal(record);
            var isAbnormal = abnormalItems.Any();

            if (isAbnormal)
            {
                summary.HasAbnormal = true;
                
                // 添加异常描述
                if (abnormalItems.Contains("体温"))
                    summary.AbnormalDescriptions.Add($"体温{record.Temperature}°C");
                if (abnormalItems.Contains("血压"))
                    summary.AbnormalDescriptions.Add($"血压{record.SysBp}/{record.DiaBp}mmHg");
                if (abnormalItems.Contains("脉搏"))
                    summary.AbnormalDescriptions.Add($"脉搏{record.Pulse}次/分");
                if (abnormalItems.Contains("血氧"))
                    summary.AbnormalDescriptions.Add($"血氧{record.Spo2}%");
            }

            summary.Records.Add(new VitalSignRecordDto
            {
                Id = record.Id,
                RecordTime = record.RecordTime,
                RecorderNurseName = record.RecorderNurse?.Name ?? "未知",
                Temperature = record.Temperature,
                TempType = record.TempType,
                Pulse = record.Pulse,
                Respiration = record.Respiration,
                SysBp = record.SysBp,
                DiaBp = record.DiaBp,
                Spo2 = record.Spo2,
                PainScore = record.PainScore,
                Weight = record.Weight,
                Intervention = record.Intervention,
                IsAbnormal = isAbnormal,
                AbnormalItems = abnormalItems
            });
        }

        // 去重异常描述
        summary.AbnormalDescriptions = summary.AbnormalDescriptions.Distinct().ToList();

        return summary;
    }

    /// <summary>
    /// 构建检查报告汇总
    /// </summary>
    private ExamReportsSummaryDto BuildExamReportsSummary(List<InspectionReport> reports)
    {
        return new ExamReportsSummaryDto
        {
            TotalCount = reports.Count,
            Reports = reports.Select(r => new InspectionReportDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                ItemName = r.InspectionOrder?.ItemName ?? "未知检查",
                ReportTime = r.ReportTime,
                ReportStatus = r.ReportStatus,
                Findings = r.Findings,
                Impression = r.Impression,
                AttachmentUrl = r.AttachmentUrl,
                ReviewerName = r.Reviewer?.Name
            }).ToList()
        };
    }

    /// <summary>
    /// 获取医嘱内容 (根据不同类型提取关键信息)
    /// </summary>
    private string GetOrderContent(Core.Models.Medical.MedicalOrder order)
    {
        return order.OrderType switch
        {
            "MedicationOrder" => GetMedicationOrderContent(order as MedicationOrder),
            "InspectionOrder" => (order as InspectionOrder)?.ItemName ?? "检查医嘱",
            "OperationOrder" => (order as OperationOrder)?.OperationName ?? "操作医嘱",
            "SurgicalOrder" => (order as SurgicalOrder)?.SurgeryName ?? "手术医嘱",
            "DischargeOrder" => "出院医嘱",
            _ => "医嘱"
        };
    }

    /// <summary>
    /// 获取药品医嘱内容 (处理多药组合)
    /// </summary>
    private string GetMedicationOrderContent(MedicationOrder? medOrder)
    {
        if (medOrder == null || medOrder.Items == null || !medOrder.Items.Any())
        {
            return "药品医嘱";
        }

        // 如果只有一个药品，直接返回药品名
        if (medOrder.Items.Count == 1)
        {
            return medOrder.Items.First().Drug?.GenericName ?? "药品医嘱";
        }

        // 如果有多个药品，组合显示
        var drugNames = medOrder.Items
            .Select(item => item.Drug?.GenericName ?? "未知药品")
            .Take(3); // 最多显示前3个
        
        var result = string.Join(" + ", drugNames);
        if (medOrder.Items.Count > 3)
        {
            result += $" 等{medOrder.Items.Count}种药品";
        }

        return result;
    }

    /// <summary>
    /// 获取医嘱规格/剂量
    /// </summary>
    private string? GetOrderSpecification(Core.Models.Medical.MedicalOrder order)
    {
        if (order is MedicationOrder medOrder && medOrder.Items != null && medOrder.Items.Any())
        {
            // 对于药品医嘱，显示第一个药品的剂量和用法
            var firstItem = medOrder.Items.First();
            return $"{firstItem.Dosage} {medOrder.UsageRoute} {medOrder.TimingStrategy}";
        }
        return null;
    }

    /// <summary>
    /// 检查生命体征是否异常
    /// </summary>
    private List<string> CheckVitalSignsAbnormal(VitalSignsRecord record)
    {
        var abnormalItems = new List<string>();

        // 体温异常 (>38.5°C 或 <35°C)
        if (record.Temperature > 38.5m || record.Temperature < 35m)
            abnormalItems.Add("体温");

        // 血压异常 (收缩压>140 或 <90, 舒张压>90 或 <60)
        if (record.SysBp > 140 || record.SysBp < 90 || record.DiaBp > 90 || record.DiaBp < 60)
            abnormalItems.Add("血压");

        // 脉搏异常 (<60 或 >100)
        if (record.Pulse < 60 || record.Pulse > 100)
            abnormalItems.Add("脉搏");

        // 血氧异常 (<95%)
        if (record.Spo2 < 95m)
            abnormalItems.Add("血氧");

        return abnormalItems;
    }

    /// <summary>
    /// 获取医嘱是否为长期医嘱
    /// </summary>
    private bool GetOrderIsLongTerm(Core.Models.Medical.MedicalOrder order)
    {
        return order.OrderType switch
        {
            "MedicationOrder" => (order as MedicationOrder)?.IsLongTerm ?? false,
            "InspectionOrder" => false, // 检查医嘱通常为临时
            "OperationOrder" => false,  // 操作医嘱通常为临时
            "SurgicalOrder" => false,   // 手术医嘱通常为临时
            "DischargeOrder" => false,  // 出院医嘱为临时
            _ => false
        };
    }

    /// <summary>
    /// 获取医嘱摘要/备注
    /// </summary>
    private string? GetOrderSummary(Core.Models.Medical.MedicalOrder order)
    {
        return order.Remarks;
    }

    /// <summary>
    /// 获取医嘱计划结束时间 (主要针对长期医嘱)
    /// </summary>
    private DateTime? GetOrderPlannedEndTime(Core.Models.Medical.MedicalOrder order)
    {
        if (order.IsLongTerm)
        {
            return order.PlantEndTime;
        }
        return null;
    }

    /// <summary>
    /// 获取出院医嘱的预计出院时间
    /// </summary>
    private DateTime? GetOrderDischargeTime(Core.Models.Medical.MedicalOrder order)
    {
        if (order is DischargeOrder dischargeOrder)
        {
            return dischargeOrder.DischargeTime;
        }
        return null;
    }
}
