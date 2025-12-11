using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.Interfaces;
using CareFlow.Application.Services.MedicalOrder;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Application.Services;

public class InspectionService : IInspectionService
{
    private readonly IRepository<InspectionOrder, long> _orderRepo;
    private readonly IRepository<InspectionReport, long> _reportRepo;
    private readonly IRepository<Patient, string> _patientRepo;
    private readonly IRepository<Doctor, string> _doctorRepo;
    private readonly IRepository<Nurse, string> _nurseRepo;
    private readonly IRepository<ExecutionTask, long> _taskRepo;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepo;
    private readonly IBarcodeService _barcodeService;

    public InspectionService(
        IRepository<InspectionOrder, long> orderRepo,
        IRepository<InspectionReport, long> reportRepo,
        IRepository<Patient, string> patientRepo,
        IRepository<Doctor, string> doctorRepo,
        IRepository<Nurse, string> nurseRepo,
        IRepository<ExecutionTask, long> taskRepo,
        IRepository<BarcodeIndex, string> barcodeRepo,
        IBarcodeService barcodeService)
    {
        _orderRepo = orderRepo;
        _reportRepo = reportRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _nurseRepo = nurseRepo;
        _taskRepo = taskRepo;
        _barcodeRepo = barcodeRepo;
        _barcodeService = barcodeService;
    }

    // ===== 检查医嘱状态管理(内部使用) =====

    public async Task UpdateInspectionStatusAsync(UpdateInspectionStatusDto dto)
    {
        var order = await _orderRepo.GetByIdAsync(dto.OrderId);
        if (order == null) throw new Exception("检查医嘱不存在");

        order.InspectionStatus = dto.Status;
        
        // 根据状态更新相应的时间戳
        var timestamp = dto.Timestamp ?? DateTime.UtcNow;
        switch (dto.Status)
        {
            case InspectionOrderStatus.InProgress:
                order.CheckStartTime = timestamp;
                break;
            case InspectionOrderStatus.BackToWard:
                if (!order.CheckEndTime.HasValue)
                    order.CheckEndTime = timestamp;
                order.BackToWardTime = timestamp;
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
            RisLisId = order.RisLisId,
            Location = order.Location,
            Source = order.Source,
            InspectionStatus = order.InspectionStatus,
            AppointmentTime = order.AppointmentTime,
            AppointmentPlace = order.AppointmentPlace,
            Precautions = order.Precautions,
            CheckStartTime = order.CheckStartTime,
            CheckEndTime = order.CheckEndTime,
            BackToWardTime = order.BackToWardTime,
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

        // ✅ 步骤7完成后:检查站护士发送报告,自动生成"查看报告"任务
        var taskService = new InspectionOrderTaskService(_taskRepo, _barcodeRepo, _barcodeService);
        await taskService.CreateReportReviewTask(order, report.Id);

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
            InspectionOrderStatus.InProgress => "检查中",
            InspectionOrderStatus.BackToWard => "已回病房",
            InspectionOrderStatus.ReportCompleted => "报告已出",
            InspectionOrderStatus.Cancelled => "已取消",
            _ => "未知状态"
        };
    }

    // ===== 新的工作流方法 =====

    /// <summary>
    /// 病房护士发送检查申请到检查站
    /// </summary>
    public async Task SendInspectionRequestAsync(SendInspectionRequestDto dto)
    {
        var order = await _orderRepo.GetByIdAsync(dto.OrderId);

        if (order == null)
            throw new Exception($"未找到检查医嘱 {dto.OrderId}");

        if (order.InspectionStatus != InspectionOrderStatus.Pending)
            throw new Exception($"检查医嘱状态异常: {order.InspectionStatus}");

        // 更新医嘱状态并记录检查站ID
        order.Location = dto.InspectionStationId;
        await _orderRepo.UpdateAsync(order);
    }

    /// <summary>
    /// 检查站护士查看待处理的检查申请列表
    /// </summary>
    public async Task<List<InspectionRequestDto>> GetPendingRequestsAsync(string inspectionStationId)
    {
        var orders = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
                .ThenInclude(p => p.Bed)
            .Where(o => o.Location == inspectionStationId)
            .Where(o => o.InspectionStatus == InspectionOrderStatus.Pending)
            .Where(o => o.AppointmentTime == null)
            .OrderBy(o => o.CreateTime)
            .ToListAsync();

        return orders.Select(o => new InspectionRequestDto
        {
            OrderId = o.Id,
            PatientId = o.PatientId,
            PatientName = o.Patient.Name,
            BedNumber = o.Patient.Bed.Id,
            ItemCode = o.ItemCode,
            ItemName = GetInspectionItemName(o.ItemCode),
            CreateTime = o.CreateTime,
            Status = o.InspectionStatus
        }).ToList();
    }

    /// <summary>
    /// 检查站护士创建预约(自动调用 InspectionOrderTaskService 生成3个执行任务)
    /// </summary>
    public async Task CreateAppointmentAsync(CreateAppointmentDto dto)
    {
        // 查询医嘱并包含Patient，用于任务生成
        var order = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
            throw new Exception($"未找到检查医嘱 {dto.OrderId}");

        // 更新预约信息
        order.AppointmentTime = dto.AppointmentTime;
        order.AppointmentPlace = dto.AppointmentPlace;
        order.Precautions = dto.Precautions;
        await _orderRepo.UpdateAsync(order);

        // 步骤3完成后:检查站护士创建预约,自动生成3个执行任务
        var taskService = new InspectionOrderTaskService(_taskRepo, _barcodeRepo, _barcodeService);
        await taskService.CreateTasks(order);
    }

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

        if (task.Status != ExecutionTaskStatus.Pending.ToString())
            throw new Exception($"任务状态异常: {task.Status}");

        // 根据 DataPayload 中的任务类型判断
        string payload = task.DataPayload;

        if (payload.Contains("INSP_PRINT"))
        {
            // 打印导引单任务
            task.Status = ExecutionTaskStatus.Completed.ToString();
            task.ActualStartTime = DateTime.UtcNow;
            task.ActualEndTime = DateTime.UtcNow;
            task.ExecutorStaffId = dto.NurseId;
            await _taskRepo.UpdateAsync(task);

            return new { message = "✅ 导引单已打印", taskType = "print" };
        }
        else if (payload.Contains("INSP_CHECKIN"))
        {
            // 签到任务
            var order = await _orderRepo.GetByIdAsync(task.MedicalOrderId);
            if (order != null)
            {
                order.InspectionStatus = InspectionOrderStatus.InProgress;
                order.CheckStartTime = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
            }

            task.Status = ExecutionTaskStatus.Completed.ToString();
            task.ActualStartTime = DateTime.UtcNow;
            task.ActualEndTime = DateTime.UtcNow;
            task.ExecutorStaffId = dto.NurseId;
            await _taskRepo.UpdateAsync(task);

            return new { message = "✅ 签到成功,患者已开始检查", taskType = "checkin" };
        }
        else if (payload.Contains("INSP_COMPLETE"))
        {
            // 检查完成任务
            var order = await _orderRepo.GetByIdAsync(task.MedicalOrderId);
            if (order != null)
            {
                order.InspectionStatus = InspectionOrderStatus.BackToWard;
                order.CheckEndTime = DateTime.UtcNow;
                order.BackToWardTime = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
            }

            task.Status = ExecutionTaskStatus.Completed.ToString();
            task.ActualStartTime = DateTime.UtcNow;
            task.ActualEndTime = DateTime.UtcNow;
            task.ExecutorStaffId = dto.NurseId;
            await _taskRepo.UpdateAsync(task);

            return new { message = "✅ 检查完成,患者可返回病房", taskType = "complete" };
        }

        throw new Exception("未知的任务类型");
    }

    private string GetInspectionItemName(string itemCode)
    {
        // 简单映射,实际应该从数据字典获取
        return itemCode switch
        {
            "CT001" => "头部CT",
            "MRI001" => "腰椎MRI",
            "XR001" => "胸部X光",
            "US001" => "腹部B超",
            "BLOOD001" => "血常规",
            _ => itemCode
        };
    }
}
