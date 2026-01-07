using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Patient;

/// <summary>
/// 患者日志查询请求DTO
/// </summary>
public class PatientLogQueryDto
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 开始日期 (默认：前天)
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// 结束日期 (默认：今天23:59:59)
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// 内容类型筛选 (可多选)
    /// 可选值: "MedicalOrders", "NursingRecords", "ExamReports"
    /// </summary>
    public List<string> ContentTypes { get; set; } = new();
}

/// <summary>
/// 患者日志响应DTO
/// </summary>
public class PatientLogResponseDto
{
    /// <summary>
    /// 患者基本信息
    /// </summary>
    public PatientBasicInfoDto Patient { get; set; } = null!;
    
    /// <summary>
    /// 按日期分组的日志数据
    /// </summary>
    public List<DailyLogDto> DailyLogs { get; set; } = new();
}

/// <summary>
/// 每日日志DTO
/// </summary>
public class DailyLogDto
{
    /// <summary>
    /// 日期 (格式: yyyy-MM-dd)
    /// </summary>
    public string Date { get; set; } = null!;
    
    /// <summary>
    /// 医嘱执行汇总 (当天有执行记录时才有值)
    /// </summary>
    public MedicalOrdersSummaryDto? MedicalOrdersSummary { get; set; }
    
    /// <summary>
    /// 护理记录汇总 (当天有护理记录时才有值)
    /// </summary>
    public NursingRecordsSummaryDto? NursingRecordsSummary { get; set; }
    
    /// <summary>
    /// 检查报告汇总 (当天有报告发布时才有值)
    /// </summary>
    public ExamReportsSummaryDto? ExamReportsSummary { get; set; }
}

/// <summary>
/// 医嘱执行汇总DTO
/// </summary>
public class MedicalOrdersSummaryDto
{
    /// <summary>
    /// 当天执行的医嘱总数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 医嘱执行详情列表 (按医嘱分组，同一医嘱的多次执行任务归集在一起)
    /// </summary>
    public List<ExecutionRecordDto> Records { get; set; } = new();
}

/// <summary>
/// 单条医嘱执行记录DTO (代表一条医嘱及其当天的所有执行任务)
/// </summary>
public class ExecutionRecordDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱类型 (MedicationOrder/InspectionOrder/OperationOrder/SurgicalOrder)
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 医嘱内容 (药品名/检查项/操作名)
    /// </summary>
    public string OrderContent { get; set; } = null!;
    
    /// <summary>
    /// 医嘱规格/剂量 (可选)
    /// </summary>
    public string? Specification { get; set; }
    
    /// <summary>
    /// 是否为长期医嘱
    /// </summary>
    public bool IsLongTerm { get; set; }
    
    /// <summary>
    /// 医嘱摘要/备注
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// 计划结束时间 (长期医嘱)
    /// </summary>
    public DateTime? PlannedEndTime { get; set; }
    
    /// <summary>
    /// 预计出院时间 (仅出院医嘱使用)
    /// </summary>
    public DateTime? DischargeTime { get; set; }
    
    /// <summary>
    /// 该医嘱在当天的执行任务列表
    /// </summary>
    public List<ExecutionTaskSummaryDto> Tasks { get; set; } = new();
}

/// <summary>
/// 执行任务摘要DTO (轻量级，仅用于列表展示)
/// </summary>
public class ExecutionTaskSummaryDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 实际执行开始时间
    /// </summary>
    public DateTime? ActualStartTime { get; set; }
    
    /// <summary>
    /// 实际执行结束时间 (持续类任务才有)
    /// </summary>
    public DateTime? ActualEndTime { get; set; }
    
    /// <summary>
    /// 执行护士姓名
    /// </summary>
    public string? ExecutorName { get; set; }
    
    /// <summary>
    /// 负责护士姓名
    /// </summary>
    public string? AssignedNurseName { get; set; }
    
    /// <summary>
    /// 任务数据载荷 (JSON字符串，包含Title等信息)
    /// </summary>
    public string? DataPayload { get; set; }
    
    /// <summary>
    /// 任务类别
    /// </summary>
    public TaskCategory Category { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    public ExecutionTaskStatus Status { get; set; }
    
    /// <summary>
    /// 执行结果（仅ResultPending类任务有值）
    /// 记录测量数值、检测数据、皮试结果等
    /// </summary>
    public string? ResultPayload { get; set; }
    
    /// <summary>
    /// 执行备注（所有任务类型都可填写）
    /// 记录执行过程中的备注、观察、特殊情况说明
    /// </summary>
    public string? ExecutionRemarks { get; set; }
}

/// <summary>
/// 护理记录汇总DTO
/// </summary>
public class NursingRecordsSummaryDto
{
    /// <summary>
    /// 当天护理记录总数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 是否包含异常数据 (高烧、高血压等)
    /// </summary>
    public bool HasAbnormal { get; set; }
    
    /// <summary>
    /// 异常描述列表 (如："体温39.2°C", "收缩压160mmHg")
    /// </summary>
    public List<string> AbnormalDescriptions { get; set; } = new();
    
    /// <summary>
    /// 护理记录详情列表
    /// </summary>
    public List<VitalSignRecordDto> Records { get; set; } = new();
}

/// <summary>
/// 单条护理记录DTO (生命体征记录)
/// </summary>
public class VitalSignRecordDto
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 记录时间
    /// </summary>
    public DateTime RecordTime { get; set; }
    
    /// <summary>
    /// 记录护士姓名
    /// </summary>
    public string RecorderNurseName { get; set; } = null!;
    
    // 体征数据
    public decimal Temperature { get; set; }
    public string TempType { get; set; } = null!;
    public int Pulse { get; set; }
    public int Respiration { get; set; }
    public int SysBp { get; set; }
    public int DiaBp { get; set; }
    public decimal Spo2 { get; set; }
    public int PainScore { get; set; }
    public decimal Weight { get; set; }
    public string? Intervention { get; set; }
    
    /// <summary>
    /// 是否异常
    /// </summary>
    public bool IsAbnormal { get; set; }
    
    /// <summary>
    /// 异常项目列表 (如: ["体温", "血压"])
    /// </summary>
    public List<string> AbnormalItems { get; set; } = new();
}

/// <summary>
/// 检查报告汇总DTO
/// </summary>
public class ExamReportsSummaryDto
{
    /// <summary>
    /// 当天发布的报告份数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 检查报告详情列表
    /// </summary>
    public List<InspectionReportDto> Reports { get; set; } = new();
}

/// <summary>
/// 单条检查报告DTO
/// </summary>
public class InspectionReportDto
{
    /// <summary>
    /// 报告ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 关联的检查医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 检查项目名称
    /// </summary>
    public string ItemName { get; set; } = null!;
    
    /// <summary>
    /// 报告时间
    /// </summary>
    public DateTime ReportTime { get; set; }
    
    /// <summary>
    /// 报告状态
    /// </summary>
    public InspectionReportStatus ReportStatus { get; set; }
    
    /// <summary>
    /// 检查所见
    /// </summary>
    public string? Findings { get; set; }
    
    /// <summary>
    /// 诊断意见/印象
    /// </summary>
    public string? Impression { get; set; }
    
    /// <summary>
    /// 报告附件URL (影像PDF/图片链接)
    /// </summary>
    public string? AttachmentUrl { get; set; }
    
    /// <summary>
    /// 审核医生姓名
    /// </summary>
    public string? ReviewerName { get; set; }
}

/// <summary>
/// 患者基本信息DTO (用于患者日志顶部展示)
/// </summary>
public class PatientBasicInfoDto
{
    public string PatientId { get; set; } = null!;
    public string PatientName { get; set; } = null!;
    public string BedId { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public int Age { get; set; }
    public string NursingGrade { get; set; } = null!;
}
