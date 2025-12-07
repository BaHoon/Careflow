using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
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

    public InspectionService(
        IRepository<InspectionOrder, long> orderRepo,
        IRepository<InspectionReport, long> reportRepo,
        IRepository<Patient, string> patientRepo,
        IRepository<Doctor, string> doctorRepo,
        IRepository<Nurse, string> nurseRepo)
    {
        _orderRepo = orderRepo;
        _reportRepo = reportRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _nurseRepo = nurseRepo;
    }

    // ===== 检查医嘱相关 =====

    public async Task<long> CreateInspectionOrderAsync(CreateInspectionOrderDto dto)
    {
        // 验证患者和医生是否存在
        var patient = await _patientRepo.GetByIdAsync(dto.PatientId);
        if (patient == null) throw new Exception("患者不存在");

        var doctor = await _doctorRepo.GetByIdAsync(dto.DoctorId);
        if (doctor == null) throw new Exception("医生不存在");

        var order = new InspectionOrder
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            ItemCode = dto.ItemCode,
            Location = dto.Location,
            Source = dto.Source,
            IsLongTerm = dto.IsLongTerm,
            PlantEndTime = dto.PlantEndTime,
            RisLisId = GenerateRisLisId(dto.Source),
            Status = "Active",  // 基类的通用状态
            OrderType = "Inspection",
            InspectionStatus = InspectionOrderStatus.Pending,
            CreateTime = DateTime.UtcNow
        };

        await _orderRepo.AddAsync(order);
        return order.Id;
    }

    public async Task UpdateAppointmentAsync(UpdateAppointmentDto dto)
    {
        var order = await _orderRepo.GetByIdAsync(dto.OrderId);
        if (order == null) throw new Exception("检查医嘱不存在");

        order.AppointmentTime = dto.AppointmentTime;
        order.AppointmentPlace = dto.AppointmentPlace;
        order.Precautions = dto.Precautions;
        // 预约信息更新后，状态仍为 Pending，等待患者前往

        await _orderRepo.UpdateAsync(order);
    }

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

    public async Task<InspectionGuideDto> GenerateInspectionGuideAsync(long orderId)
    {
        var order = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) throw new Exception("检查医嘱不存在");

        return new InspectionGuideDto
        {
            OrderId = order.Id,
            PatientName = order.Patient.Name,
            PatientId = order.PatientId,
            ItemCode = order.ItemCode,
            AppointmentTime = order.AppointmentTime,
            AppointmentPlace = order.AppointmentPlace,
            Precautions = order.Precautions,
            Location = order.Location
        };
    }

    public async Task<List<InspectionOrderDetailDto>> GetPatientInspectionOrdersAsync(string patientId)
    {
        var orders = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreateTime)
            .ToListAsync();

        return orders.Select(order => new InspectionOrderDetailDto
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
        }).ToList();
    }

    public async Task<List<NurseInspectionBoardDto>> GetNurseBoardDataAsync(string wardId)
    {
        var orders = await _orderRepo.GetQueryable()
            .Include(o => o.Patient)
            .ThenInclude(p => p.Bed)
            .Where(o => o.Patient.Bed != null && o.Patient.Bed.WardId == wardId)
            .Where(o => o.InspectionStatus != InspectionOrderStatus.Cancelled && 
                       o.InspectionStatus != InspectionOrderStatus.ReportCompleted)
            .OrderBy(o => o.AppointmentTime)
            .ToListAsync();

        return orders.Select(order => new NurseInspectionBoardDto
        {
            OrderId = order.Id,
            PatientName = order.Patient.Name,
            BedNumber = order.Patient.Bed?.Id ?? "未分配",
            ItemCode = order.ItemCode,
            AppointmentTime = order.AppointmentTime,
            Status = order.InspectionStatus,
            StatusDisplay = GetStatusDisplayText(order.InspectionStatus)
        }).ToList();
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

    public async Task<List<InspectionReportDetailDto>> GetReportsByOrderIdAsync(long orderId)
    {
        var reports = await _reportRepo.GetQueryable()
            .Include(r => r.Patient)
            .Include(r => r.InspectionOrder)
            .Include(r => r.Reviewer)
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();

        return reports.Select(report => new InspectionReportDetailDto
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
        }).ToList();
    }

    public async Task UpdateReportStatusAsync(UpdateReportStatusDto dto)
    {
        var report = await _reportRepo.GetByIdAsync(dto.ReportId);
        if (report == null) throw new Exception("检查报告不存在");

        report.ReportStatus = dto.Status;
        await _reportRepo.UpdateAsync(report);
    }

    // ===== 测试数据生成 =====

    public async Task GenerateMockInspectionOrdersAsync()
    {
        // 获取第一个患者和医生用于测试
        var patient = await _patientRepo.GetQueryable().FirstOrDefaultAsync();
        var doctor = await _doctorRepo.GetQueryable().FirstOrDefaultAsync();

        if (patient == null || doctor == null)
        {
            throw new Exception("系统中没有患者或医生数据，请先初始化基础数据");
        }

        var mockOrders = new List<InspectionOrder>
        {
            // CT检查
            new InspectionOrder
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ItemCode = "CT001",
                Location = "放射科CT室",
                Source = InspectionSource.RIS,
                RisLisId = GenerateRisLisId(InspectionSource.RIS),
                IsLongTerm = false,
                PlantEndTime = DateTime.UtcNow.AddDays(1),
                Status = "Active",
                OrderType = "Inspection",
                InspectionStatus = InspectionOrderStatus.Pending,
                AppointmentTime = DateTime.UtcNow.AddHours(2),
                AppointmentPlace = "3号楼2层CT室1号机",
                Precautions = "检查前需脱去金属物品",
                CreateTime = DateTime.UtcNow
            },
            // 核磁检查
            new InspectionOrder
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ItemCode = "MRI001",
                Location = "放射科MRI室",
                Source = InspectionSource.RIS,
                RisLisId = GenerateRisLisId(InspectionSource.RIS),
                IsLongTerm = false,
                PlantEndTime = DateTime.UtcNow.AddDays(2),
                Status = "Active",
                OrderType = "Inspection",
                InspectionStatus = InspectionOrderStatus.Pending,
                CreateTime = DateTime.UtcNow
            },
            // 抽血检查
            new InspectionOrder
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ItemCode = "LAB001",
                Location = "检验科采血室",
                Source = InspectionSource.LIS,
                RisLisId = GenerateRisLisId(InspectionSource.LIS),
                IsLongTerm = false,
                PlantEndTime = DateTime.UtcNow.AddHours(12),
                Status = "Active",
                OrderType = "Inspection",
                InspectionStatus = InspectionOrderStatus.Pending,
                AppointmentTime = DateTime.UtcNow.AddHours(1),
                AppointmentPlace = "1号楼1层采血室3号窗口",
                Precautions = "需空腹8小时以上",
                CreateTime = DateTime.UtcNow
            },
            // 尿检
            new InspectionOrder
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ItemCode = "LAB002",
                Location = "检验科",
                Source = InspectionSource.LIS,
                RisLisId = GenerateRisLisId(InspectionSource.LIS),
                IsLongTerm = false,
                PlantEndTime = DateTime.UtcNow.AddHours(8),
                Status = "Active",
                OrderType = "Inspection",
                InspectionStatus = InspectionOrderStatus.BackToWard,
                AppointmentTime = DateTime.UtcNow.AddMinutes(-30),
                AppointmentPlace = "1号楼1层检验科",
                CheckStartTime = DateTime.UtcNow.AddMinutes(-20),
                CheckEndTime = DateTime.UtcNow.AddMinutes(-10),
                BackToWardTime = DateTime.UtcNow.AddMinutes(-5),
                CreateTime = DateTime.UtcNow.AddHours(-1)
            },
            // B超检查
            new InspectionOrder
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ItemCode = "US001",
                Location = "超声科",
                Source = InspectionSource.RIS,
                RisLisId = GenerateRisLisId(InspectionSource.RIS),
                IsLongTerm = false,
                PlantEndTime = DateTime.UtcNow.AddDays(1),
                Status = "Active",
                OrderType = "Inspection",
                InspectionStatus = InspectionOrderStatus.Pending,
                AppointmentTime = DateTime.UtcNow.AddHours(3),
                AppointmentPlace = "2号楼3层超声科2号诊室",
                Precautions = "检查前需憋尿",
                CreateTime = DateTime.UtcNow
            }
        };

        foreach (var order in mockOrders)
        {
            await _orderRepo.AddAsync(order);
        }
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
}
