using CareFlow.Core.Enums;
/// inspection下的这两个文件就是前后端数据交互的预期接口，根据实际情况还可以做出修改
namespace CareFlow.Application.DTOs.Inspection;

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
    public string ItemName { get; set; } = null!;
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
    public string ItemName { get; set; } = null!;
    public string RisLisId { get; set; } = null!;
    public string Location { get; set; } = null!;
    public InspectionSource Source { get; set; }
    public InspectionOrderStatus InspectionStatus { get; set; }
    
    public DateTime? AppointmentTime { get; set; }
    public string? AppointmentPlace { get; set; }
    public string? Precautions { get; set; }
    
    public DateTime? CheckStartTime { get; set; }
    public DateTime? CheckEndTime { get; set; }
    public DateTime? ReportPendingTime { get; set; }
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
    public string ItemName { get; set; } = null!;
    public DateTime? AppointmentTime { get; set; }
    public InspectionOrderStatus Status { get; set; }
    public string StatusDisplay { get; set; } = null!;  // 状态的中文显示
}

/// <summary>
/// 病房护士发送检查申请 DTO
/// </summary>
public class SendInspectionRequestDto
{
    /// <summary>
    /// 检查医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 目标检查站ID(如放射科、检验科等)
    /// </summary>
    public string InspectionStationId { get; set; } = string.Empty;
}

/// <summary>
/// 检查站护士创建预约 DTO
/// </summary>
public class CreateAppointmentDto
{
    /// <summary>
    /// 检查医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 预约时间
    /// </summary>
    public DateTime AppointmentTime { get; set; }
    
    /// <summary>
    /// 预约地点
    /// </summary>
    public string AppointmentPlace { get; set; } = string.Empty;
    
    /// <summary>
    /// 注意事项
    /// </summary>
    public string? Precautions { get; set; }
}

/// <summary>
/// 检查申请记录 DTO(检查站查看用)
/// </summary>
public class InspectionRequestDto
{
    public long OrderId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public InspectionOrderStatus Status { get; set; }
}

/// <summary>
/// 统一扫码 DTO(用于所有扫码场景)
/// </summary>
public class SingleScanDto
{
    /// <summary>
    /// 任务ID(从条形码/二维码解析)
    /// </summary>
    public long TaskId { get; set; }
    
    /// <summary>
    /// 患者ID(从手环扫描或任务关联)
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// 扫码护士ID
    /// </summary>
    public string NurseId { get; set; } = string.Empty;
}
