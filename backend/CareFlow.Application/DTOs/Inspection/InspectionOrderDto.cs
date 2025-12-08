using CareFlow.Core.Enums;
/// inspection下的这两个文件就是前后端数据交互的预期接口，根据实际情况还可以做出修改
namespace CareFlow.Application.DTOs.Inspection;

/// <summary>
/// 创建检查医嘱请求 DTO
/// </summary>
public class CreateInspectionOrderDto
{
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public string Location { get; set; } = null!;
    public InspectionSource Source { get; set; }
    public bool IsLongTerm { get; set; }
    public DateTime PlantEndTime { get; set; }
}

/// <summary>
/// 预约信息更新 DTO (模拟从 RIS/LIS 接收的预约反馈)
/// </summary>
public class UpdateAppointmentDto
{
    public long OrderId { get; set; }
    public DateTime AppointmentTime { get; set; }
    public string AppointmentPlace { get; set; } = null!;
    public string? Precautions { get; set; }
}

/// <summary>
/// 更新检查状态 DTO
/// </summary>
public class UpdateInspectionStatusDto
{
    public long OrderId { get; set; }
    public InspectionOrderStatus Status { get; set; }
    public DateTime? Timestamp { get; set; }  // 可选的时间戳，用于记录状态变更时间
}

/// <summary>
/// 检查导引单 DTO (用于护士站打印)
/// </summary>
public class InspectionGuideDto
{
    public long OrderId { get; set; }
    public string PatientName { get; set; } = null!;
    public string PatientId { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public DateTime? AppointmentTime { get; set; }
    public string? AppointmentPlace { get; set; }
    public string? Precautions { get; set; }
    public string Location { get; set; } = null!;
}

/// <summary>
/// 检查医嘱详情响应 DTO
/// </summary>
public class InspectionOrderDetailDto
{
    public long Id { get; set; }
    public string PatientId { get; set; } = null!;
    public string PatientName { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public string DoctorName { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public string RisLisId { get; set; } = null!;
    public string Location { get; set; } = null!;
    public InspectionSource Source { get; set; }
    public InspectionOrderStatus InspectionStatus { get; set; }
    
    public DateTime? AppointmentTime { get; set; }
    public string? AppointmentPlace { get; set; }
    public string? Precautions { get; set; }
    
    public DateTime? CheckStartTime { get; set; }
    public DateTime? CheckEndTime { get; set; }
    public DateTime? BackToWardTime { get; set; }
    public DateTime? ReportTime { get; set; }
    
    public DateTime CreateTime { get; set; }
    public bool IsLongTerm { get; set; }
}

/// <summary>
/// 护士看板中的检查项目 DTO
/// </summary>
public class NurseInspectionBoardDto
{
    public long OrderId { get; set; }
    public string PatientName { get; set; } = null!;
    public string BedNumber { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public DateTime? AppointmentTime { get; set; }
    public InspectionOrderStatus Status { get; set; }
    public string StatusDisplay { get; set; } = null!;  // 状态的中文显示
}
