using System.Text.Json;
using CareFlow.Core.Enums;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Application.Services.MedicalOrder;

/// <summary>
/// 检查类医嘱任务生成服务
/// 业务流程：
/// 1. 预约完成 → 病房护士打印导引单交给患者
/// 2. 患者自行前往检查站
/// 3. 患者到达 → 检查站护士扫码签到（系统自动更新状态为"检查中"）
/// 4. 检查完成 → 检查站护士扫码确认（系统自动更新状态为"已回病房"）
/// 5. 报告推送 → 病房护士查看报告（动态创建任务）
/// </summary>
public class InspectionOrderTaskService
{
    /// <summary>
    /// 根据检查医嘱生成护士执行任务列表
    /// </summary>
    public IReadOnlyList<ExecutionTask> CreateTasks(InspectionOrder order)
    {
        var tasks = new List<ExecutionTask>();
        
        // 如果没有预约时间，暂不生成任务
        if (!order.AppointmentTime.HasValue)
        {
            return tasks;
        }
        
        var appointmentTime = order.AppointmentTime.Value;
        
        // 任务1: 打印导引单（病房护士，预约完成后立即执行）
        tasks.Add(CreatePrintGuideTask(order, appointmentTime.AddMinutes(-60)));
        
        // 任务2: 检查站签到（检查站护士，扫码后自动完成）
        tasks.Add(CreateCheckInTask(order, appointmentTime));
        
        // 任务3: 检查完成确认（检查站护士，扫码后自动完成）
        tasks.Add(CreateCheckCompleteTask(order, appointmentTime.AddMinutes(30)));
        
        return tasks;
    }
    
    /// <summary>
    /// 任务1: 打印检查导引单（病房护士负责）
    /// </summary>
    private ExecutionTask CreatePrintGuideTask(InspectionOrder order, DateTime plannedTime)
    {
        var items = new List<object>
        {
            new { item = $"向患者说明检查项目：{order.ItemCode}", @checked = false },
            new { item = "确认患者身份信息", @checked = false }
        };
        
        // 添加预约信息
        items.Add(new { item = $"预约时间：{order.AppointmentTime:yyyy-MM-dd HH:mm}", @checked = false });
        items.Add(new { item = $"检查地点：{order.AppointmentPlace ?? order.Location}", @checked = false });
        
        // 根据注意事项添加提醒项
        if (!string.IsNullOrEmpty(order.Precautions))
        {
            if (order.Precautions.Contains("禁食") || order.Precautions.Contains("空腹"))
            {
                items.Add(new { item = "提醒患者检查前需禁食", @checked = false });
            }
            if (order.Precautions.Contains("憋尿") || order.Precautions.Contains("膀胱"))
            {
                items.Add(new { item = "提醒患者检查前需憋尿", @checked = false });
            }
            if (order.Precautions.Contains("金属"))
            {
                items.Add(new { item = "提醒患者检查时需取下金属物品", @checked = false });
            }
        }
        
        items.Add(new { item = "打印检查导引单（含二维码）", @checked = false });
        items.Add(new { item = "将导引单交给患者", @checked = false });
        items.Add(new { item = "告知患者自行前往检查站", @checked = false });
        
        return new ExecutionTask
        {
            MedicalOrderId = order.Id,
            MedicalOrder = order,
            PatientId = order.PatientId,
            Patient = order.Patient,
            PlannedStartTime = plannedTime,
            Status = ExecutionTaskStatus.Pending.ToString(),
            DataPayload = JsonSerializer.Serialize(new
            {
                TaskType = "INSP_PRINT_GUIDE",
                Title = "打印检查导引单",
                Description = $"请打印 {order.ItemCode} 检查导引单并交给患者",
                IsChecklist = true,
                Items = items
            })
        };
    }
    
    /// <summary>
    /// 任务2: 检查站签到（检查站护士扫码）
    /// 此任务在扫码时自动完成，护士只需扫码即可
    /// </summary>
    private ExecutionTask CreateCheckInTask(InspectionOrder order, DateTime plannedTime)
    {
        return new ExecutionTask
        {
            MedicalOrderId = order.Id,
            MedicalOrder = order,
            PatientId = order.PatientId,
            Patient = order.Patient,
            PlannedStartTime = plannedTime,
            Status = ExecutionTaskStatus.Pending.ToString(),
            DataPayload = JsonSerializer.Serialize(new
            {
                TaskType = "INSP_CHECKIN",
                Title = "检查站签到",
                Description = "患者到达检查站，请扫描导引单确认",
                IsChecklist = false,
                Items = new
                {
                    orderId = order.Id,
                    patientId = order.PatientId,
                    risLisId = order.RisLisId,
                    qrCodeData = $"INSPECTION_{order.Id}_{order.PatientId}",
                    scanRequired = true,
                    autoComplete = true  // 扫码后自动完成
                }
            })
        };
    }
    
    /// <summary>
    /// 任务3: 检查完成确认（检查站护士扫码）
    /// 此任务在扫码时自动完成
    /// </summary>
    private ExecutionTask CreateCheckCompleteTask(InspectionOrder order, DateTime plannedTime)
    {
        return new ExecutionTask
        {
            MedicalOrderId = order.Id,
            MedicalOrder = order,
            PatientId = order.PatientId,
            Patient = order.Patient,
            PlannedStartTime = plannedTime,
            Status = ExecutionTaskStatus.Pending.ToString(),
            DataPayload = JsonSerializer.Serialize(new
            {
                TaskType = "INSP_COMPLETE",
                Title = "检查完成确认",
                Description = "检查完成，请扫描确认患者可以离开",
                IsChecklist = false,
                Items = new
                {
                    orderId = order.Id,
                    patientId = order.PatientId,
                    risLisId = order.RisLisId,
                    qrCodeData = $"INSPECTION_{order.Id}_{order.PatientId}",
                    scanRequired = true,
                    autoComplete = true  // 扫码后自动完成
                }
            })
        };
    }
    
    /// <summary>
    /// 动态创建任务：查看检查报告（病房护士）
    /// 此方法在报告推送时调用，应在 InspectionService.CreateInspectionReportAsync 中调用
    /// </summary>
    public ExecutionTask CreateReportReviewTask(InspectionOrder order, long reportId)
    {
        return new ExecutionTask
        {
            MedicalOrderId = order.Id,
            MedicalOrder = order,
            PatientId = order.PatientId,
            Patient = order.Patient,
            PlannedStartTime = DateTime.UtcNow, // 报告到达后立即处理
            Status = ExecutionTaskStatus.Pending.ToString(),
            DataPayload = JsonSerializer.Serialize(new
            {
                TaskType = "INSP_REVIEW_REPORT",
                Title = "查看检查报告",
                Description = $"{order.ItemCode} 检查报告已出，请查看并告知患者",
                IsChecklist = true,
                Items = new List<object>
                {
                    new { item = "查看检查报告内容", @checked = false },
                    new { item = "评估报告结果", @checked = false },
                    new { item = "告知患者报告结果", @checked = false },
                    new { item = "如有异常，通知医生", @checked = false }
                }
            })
        };
    }
}