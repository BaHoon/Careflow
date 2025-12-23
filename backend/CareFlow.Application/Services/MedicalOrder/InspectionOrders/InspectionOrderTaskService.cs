using System.Text.Json;
using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.DTOs.InspectionOrders;
using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Application.Services.MedicalOrder;

/// <summary>
/// 检查类医嘱任务生成服务
/// 业务流程：
/// 1. 预约完成 → 病房护士打印导引单交给患者
/// 2. 患者自行前往检查站进行检查
/// 3. 检查时间半小时后 → 系统自动从检查站获取报告
/// 4. 报告到达 → 病房护士查看报告（动态创建任务）
/// </summary>
public class InspectionOrderTaskService : IInspectionService
{
    // Repositories for orders and reports
    private readonly IRepository<InspectionOrder, long> _orderRepo;
    private readonly IRepository<InspectionReport, long> _reportRepo;
    private readonly IRepository<Patient, string> _patientRepo;
    private readonly IRepository<Doctor, string> _doctorRepo;
    private readonly IRepository<Nurse, string> _nurseRepo;
    private readonly IRepository<ExecutionTask, long> _taskRepo;

    public InspectionOrderTaskService(
        IRepository<InspectionOrder, long> orderRepo,
        IRepository<InspectionReport, long> reportRepo,
        IRepository<Patient, string> patientRepo,
        IRepository<Doctor, string> doctorRepo,
        IRepository<Nurse, string> nurseRepo,
        IRepository<ExecutionTask, long> taskRepo)
    {
        _orderRepo = orderRepo;
        _reportRepo = reportRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _nurseRepo = nurseRepo;
        _taskRepo = taskRepo;
    }

    // ===== 任务生成相关 =====

    /// <summary>
    /// 生成检查申请任务【任务1】（签收时调用）
    /// </summary>
    public async Task<ExecutionTask> GenerateApplicationTaskAsync(InspectionOrder order)
    {
        // 加载医嘱完整信息
        var fullOrder = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        if (fullOrder == null)
            throw new Exception($"检查医嘱 {order.Id} 不存在");

        // 创建申请任务：病房护士在签收后1小时内提交检查申请
        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = TaskCategory.ApplicationWithPrint,  // 申请打印类任务：申请后打印导引单即结束
            Status = ExecutionTaskStatus.Applying,  // 待申请状态
            PlannedStartTime = DateTime.UtcNow.AddHours(1),  // 签收后1小时内申请
            CreatedAt = DateTime.UtcNow,
            DataPayload = JsonSerializer.Serialize(new
            {
                TaskType = "InspectionApplication",
                Title = "提交检查申请",
                Description = $"检查项目: {order.ItemCode}，检查位置: {order.Location ?? "待定"}",
                ItemCode = order.ItemCode,
                Location = order.Location,
                Source = order.Source,
                RisLisId = order.RisLisId,
                Instructions = "请在检查申请界面提交此检查医嘱的申请"
            })
        };

        await _taskRepo.AddAsync(task);
        return task;
    }

    /// <summary>
    /// 根据预约信息生成执行任务（预约确认后调用）
    /// </summary>
    public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(
        long orderId, AppointmentDetail appointmentDetail)
    {
        // 加载医嘱完整信息
        var order = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception($"检查医嘱 {orderId} 不存在");

        // 更新医嘱的预约信息
        order.AppointmentTime = appointmentDetail.AppointmentTime;
        order.AppointmentPlace = appointmentDetail.AppointmentPlace;
        order.Precautions = appointmentDetail.Precautions;
        order.InspectionStatus = InspectionOrderStatus.Pending;  // 待前往
        await _orderRepo.UpdateAsync(order);

        var tasks = new List<ExecutionTask>
        {
            // 任务1: 打印检查导引单（病房护士，预约后立即执行）
            CreatePrintGuideTask(order, appointmentDetail.AppointmentTime.AddMinutes(-30))
            // 检查时间半小时后自动从检查站返回报告
        };

        // 保存任务到数据库
        foreach (var task in tasks)
        {
            await _taskRepo.AddAsync(task);
        }

        return tasks;
    }

    // ===== 检查医嘱状态管理(内部使用) =====

    public async Task UpdateInspectionStatusAsync(UpdateInspectionStatusDto dto)
    {
        var order = await _orderRepo.GetByIdAsync(dto.OrderId);
        if (order == null) throw new Exception("检查医嘱不存在");

        order.InspectionStatus = dto.Status;

        var timestamp = dto.Timestamp ?? DateTime.UtcNow;
        switch (dto.Status)
        {
            case InspectionOrderStatus.ReportPending:
                order.ReportPendingTime = timestamp;
                break;
            case InspectionOrderStatus.ReportCompleted:
                order.ReportTime = timestamp;
                break;
        }

        await _orderRepo.UpdateAsync(order);
    }

    public async Task<InspectionOrderDetailDto> GetInspectionOrderDetailAsync(long orderId)
    {
        var order = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) throw new Exception("检查医嘱不存在");

        return new InspectionOrderDetailDto
        {
            Id = order.Id,
            PatientId = order.PatientId,
            PatientName = order.Patient.Name,
            DoctorId = order.DoctorId,
            DoctorName = order.Doctor.Name,
            ItemCode = order.ItemCode,
            ItemName = order.ItemName,
            RisLisId = order.RisLisId,
            Location = order.Location,
            Source = order.Source,
            InspectionStatus = order.InspectionStatus,
            AppointmentTime = order.AppointmentTime,
            AppointmentPlace = order.AppointmentPlace,
            Precautions = order.Precautions,
            CheckStartTime = order.CheckStartTime,
            CheckEndTime = order.CheckEndTime,
            ReportPendingTime = order.ReportPendingTime,
            ReportTime = order.ReportTime,
            CreateTime = order.CreateTime,
            IsLongTerm = order.IsLongTerm
        };
    }

    // ===== 检查报告相关 =====

    public async Task<long> CreateInspectionReportAsync(CreateInspectionReportDto dto)
    {
        var order = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null) throw new Exception("检查医嘱不存在");

        var report = new InspectionReport
        {
            OrderId = dto.OrderId,
            PatientId = order.PatientId,
            RisLisId = dto.RisLisId,
            ReportTime = DateTime.UtcNow,
            ReportStatus = InspectionReportStatus.Completed,
            Findings = dto.Findings,
            Impression = dto.Impression,
            AttachmentUrl = dto.AttachmentUrl,
            ReviewerId = dto.ReviewerId,
            ReportSource = dto.ReportSource,
            CreateTime = DateTime.UtcNow
        };

        await _reportRepo.AddAsync(report);

        // 更新医嘱状态
        order.InspectionStatus = InspectionOrderStatus.ReportCompleted;
        order.ReportTime = DateTime.UtcNow;
        order.ReportId = report.Id.ToString();
        await _orderRepo.UpdateAsync(order);

        // 报告创建完成后，自动生成"查看报告"任务
        await CreateReportReviewTask(order, report.Id);

        return report.Id;
    }

    public async Task<InspectionReportDetailDto> GetInspectionReportDetailAsync(long reportId)
    {
        var report = await _reportRepo.GetQueryable()
            .Include(r => r.Patient)
            .Include(r => r.InspectionOrder)
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null) throw new Exception("检查报告不存在");

        return new InspectionReportDetailDto
        {
            Id = report.Id,
            OrderId = report.OrderId,
            PatientId = report.PatientId,
            PatientName = report.Patient.Name,
            ItemCode = report.InspectionOrder.ItemCode,
            ItemName = report.InspectionOrder.ItemName,
            RisLisId = report.RisLisId,
            ReportTime = report.ReportTime,
            ReportStatus = report.ReportStatus,
            Findings = report.Findings,
            Impression = report.Impression,
            AttachmentUrl = report.AttachmentUrl,
            ReviewerId = report.ReviewerId,
            ReviewerName = report.Reviewer?.Name,
            ReportSource = report.ReportSource,
            CreateTime = report.CreateTime
        };
    }

    // ===== 扫码处理 =====

    /// <summary>
    /// 统一扫码处理(根据任务类型自动处理)
    /// </summary>
    public async Task<object> ProcessScanAsync(SingleScanDto dto)
    {
        var task = await _taskRepo.GetByIdAsync(dto.TaskId);

        if (task == null)
            throw new Exception($"未找到任务 {dto.TaskId}");

        if (task.PatientId != dto.PatientId)
            throw new Exception("患者信息不匹配");

        // 检查任务状态是否可以执行（Applying, Applied, AppliedConfirmed, Pending 状态都可以）
        if (task.Status != ExecutionTaskStatus.Applying && 
            task.Status != ExecutionTaskStatus.Applied && 
            task.Status != ExecutionTaskStatus.AppliedConfirmed && 
            task.Status != ExecutionTaskStatus.Pending)
            throw new Exception($"任务状态异常: {task.Status}");

        string payload = task.DataPayload;

        if (payload.Contains("INSP_PRINT_GUIDE"))
        {
            task.Status = ExecutionTaskStatus.Completed;
            task.ActualStartTime = DateTime.UtcNow;
            task.ActualEndTime = DateTime.UtcNow;
            task.ExecutorStaffId = dto.NurseId;
            await _taskRepo.UpdateAsync(task);

            return new { message = "✅ 导引单已打印，患者可前往检查", taskType = "print" };
        }

        throw new Exception("未知的任务类型");
    }

    // ===== 辅助方法 =====

    private string GenerateRisLisId(InspectionSource source)
    {
        var prefix = source == InspectionSource.RIS ? "RIS" : "LIS";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}{timestamp}{random}";
    }

    private string GetStatusDisplayText(InspectionOrderStatus status)
    {
        return status switch
        {
            InspectionOrderStatus.Pending => "待前往",
            InspectionOrderStatus.ReportPending => "报告待出",
            InspectionOrderStatus.ReportCompleted => "报告已出",
            InspectionOrderStatus.Cancelled => "已取消",
            _ => "未知状态"
        };
    }

    private string GetInspectionItemName(string itemCode)
    {
        // 直接返回项目代码，前端根据需要自行处理显示
        return itemCode;
    }

    /// <summary>
    /// 根据检查医嘱生成护士执行任务列表（异步方法）
    /// </summary>
    public async Task CreateTasks(InspectionOrder order)
    {
        // 如果没有预约时间，暂不生成任务
        if (!order.AppointmentTime.HasValue)
        {
            return;
        }
        
        var appointmentTime = order.AppointmentTime.Value;
        
        var tasks = new List<ExecutionTask>
        {
            // 任务1: 打印导引单（病房护士，预约完成后立即执行）
            CreatePrintGuideTask(order, appointmentTime.AddMinutes(-60))
            // 检查时间半小时后自动从检查站返回报告
        };
        
        // 保存任务到数据库
        foreach (var task in tasks)
        {
            await _taskRepo.AddAsync(task);
        }
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
            PatientId = order.PatientId,
            Category = TaskCategory.ResultPending,  // 结果等待类：打印导引单后等待检查报告返回
            PlannedStartTime = plannedTime,
            Status = ExecutionTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
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
    /// 动态创建任务：查看检查报告（病房护士）
    /// </summary>
    public async Task<ExecutionTask> CreateReportReviewTask(InspectionOrder order, long reportId)
    {
        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            MedicalOrder = order,
            PatientId = order.PatientId,
            Patient = order.Patient,
            Category = TaskCategory.DataCollection, // 查看报告为数据收集类
            PlannedStartTime = DateTime.UtcNow, // 报告到达后立即处理
            Status = ExecutionTaskStatus.Pending,
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
        
        // 保存任务到数据库
        await _taskRepo.AddAsync(task);
        
        return task;
    }
}