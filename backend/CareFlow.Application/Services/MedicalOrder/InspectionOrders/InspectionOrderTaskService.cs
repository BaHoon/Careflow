using System.Text.Json;
using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.DTOs.InspectionOrders;
using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Application.Interfaces;
using CareFlow.Application.Services.Report;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PatientModel = CareFlow.Core.Models.Organization.Patient;

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
    private readonly IRepository<PatientModel, string> _patientRepo;
    private readonly IRepository<Doctor, string> _doctorRepo;
    private readonly IRepository<Nurse, string> _nurseRepo;
    private readonly IRepository<ExecutionTask, long> _taskRepo;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly InspectionReportPdfService _pdfService;
    private readonly IWebHostEnvironment _environment;
    private readonly ICareFlowDbContext _context;

    public InspectionOrderTaskService(
        IRepository<InspectionOrder, long> orderRepo,
        IRepository<InspectionReport, long> reportRepo,
        IRepository<PatientModel, string> patientRepo,
        IRepository<Doctor, string> doctorRepo,
        IRepository<Nurse, string> nurseRepo,
        IRepository<ExecutionTask, long> taskRepo,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        InspectionReportPdfService pdfService,
        IWebHostEnvironment environment,
        ICareFlowDbContext context)
    {
        _orderRepo = orderRepo;
        _reportRepo = reportRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _nurseRepo = nurseRepo;
        _taskRepo = taskRepo;
        _statusHistoryRepository = statusHistoryRepository;
        _pdfService = pdfService;
        _environment = environment;
        _context = context;
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

        // 创建申请任务：病房护士签收后立即提交检查申请，打印导引单后任务结束
        var task = new ExecutionTask
        {
            MedicalOrderId = order.Id,
            PatientId = order.PatientId,
            Category = TaskCategory.ApplicationWithPrint,  // 申请打印类任务
            Status = ExecutionTaskStatus.Applying,  // 待申请状态
            PlannedStartTime = DateTime.UtcNow.AddMinutes(1),  // 签收后1分钟内申请
            CreatedAt = DateTime.UtcNow,
            DataPayload = JsonSerializer.Serialize(new
            {
                TaskType = "InspectionApplication",
                Title = $"检查申请：{order.ItemName}",
                Description = $"检查项目: {order.ItemCode}，检查位置: {order.Location ?? "待定"}",
                ItemCode = order.ItemCode,
                Location = order.Location,
                Source = order.Source,
                RisLisId = order.RisLisId,
                Instructions = "请提交检查申请并打印导引单，打印完成后此任务结束"
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

        // 预约确认后不再生成新的任务，检查申请任务在打印导引单后结束
        // 检查报告在打印导引单结束后才可能返回
        return new List<ExecutionTask>();
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
                .ThenInclude(p => p.Bed)
                    .ThenInclude(b => b.Ward)
                        .ThenInclude(w => w.Department)
            .Include(o => o.Doctor)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null) throw new Exception("检查医嘱不存在");

        var patient = order.Patient;
        
        // 生成报告编号
        var reportNumber = $"R{DateTime.Now:yyyyMMddHHmmss}{order.Id}";
        
        // 生成 PDF 文件路径
        var reportsFolder = Path.Combine(_environment.WebRootPath, "reports");
        if (!Directory.Exists(reportsFolder))
        {
            Directory.CreateDirectory(reportsFolder);
        }
        
        var fileName = $"Report_{reportNumber}.pdf";
        var filePath = Path.Combine(reportsFolder, fileName);
        var relativeUrl = $"reports/{fileName}";
        
        // 准备报告数据
        var reportData = new InspectionReportData
        {
            HospitalName = "医疗机构",
            PatientId = patient.Id,
            PatientName = patient.Name,
            PatientGender = patient.Gender,
            PatientAge = CalculateAge(patient.DateOfBirth),
            Department = patient.Bed?.Ward?.Department?.DeptName ?? "未知科室",
            BedNumber = patient.Bed?.Id ?? "未分配",
            RequestingDoctor = order.Doctor?.Name ?? "未知医生",
            ItemCode = order.ItemCode,
            ItemName = order.ItemName,
            RisLisId = dto.RisLisId,
            // 将UTC时间转换为中国时区（UTC+8）
            ExaminationDate = TimeZoneInfo.ConvertTimeFromUtc(
                order.AppointmentTime ?? order.CreateTime, 
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")),
            ReportDate = DateTime.Now,
            Findings = dto.Findings,
            Impression = dto.Impression,
            ReviewerName = dto.ReviewerId != null ? 
                (await _doctorRepo.GetByIdAsync(dto.ReviewerId))?.Name : "检查站",
            ReportNumber = reportNumber
        };
        
        // 生成 PDF
        try
        {
            _pdfService.GenerateReportPdf(reportData, filePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"生成 PDF 报告失败: {ex.Message}", ex);
        }

        // 创建报告记录
        var report = new InspectionReport
        {
            OrderId = dto.OrderId,
            PatientId = order.PatientId,
            RisLisId = dto.RisLisId,
            ReportTime = DateTime.UtcNow,
            ReportStatus = InspectionReportStatus.Completed,
            Findings = dto.Findings,
            Impression = dto.Impression,
            AttachmentUrl = relativeUrl,  // 使用生成的 PDF 路径
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

        // 不再自动生成"查看报告"任务，用户可以在检查申请任务中直接查看
        // await CreateReportReviewTask(order, report.Id);

        return report.Id;
    }
    
    /// <summary>
    /// 计算年龄
    /// </summary>
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
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
            
            // ==================== 检查并更新医嘱状态 ====================
            // 当任务完成时，如果医嘱状态是Accepted，则更新为InProgress
            if (task.MedicalOrderId > 0)
            {
                var medicalOrder = await _context.Set<CareFlow.Core.Models.Medical.MedicalOrder>()
                    .FirstOrDefaultAsync(o => o.Id == task.MedicalOrderId);
                
                if (medicalOrder != null && medicalOrder.Status == OrderStatus.Accepted)
                {
                    var originalStatus = medicalOrder.Status;
                    medicalOrder.Status = OrderStatus.InProgress;
                    
                    // 添加医嘱状态变更历史记录
                    var history = new MedicalOrderStatusHistory
                    {
                        MedicalOrderId = medicalOrder.Id,
                        FromStatus = originalStatus,
                        ToStatus = OrderStatus.InProgress,
                        ChangedAt = DateTime.UtcNow,
                        ChangedById = dto.NurseId,
                        ChangedByType = "Nurse",
                        Reason = "护士执行检查任务，医嘱状态自动更新为执行中"
                    };
                    await _statusHistoryRepository.AddAsync(history);
                }
            }
            
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
        // 不再自动生成任务，只在签收时生成检查申请任务
        // 预约确认后不再生成打印导引单任务
        await Task.CompletedTask;
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
                Title = $"打印导引单：{order.ItemCode}",
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