using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Patient;

/// <summary>
/// 患者卡片DTO - 用于患者管理界面列表展示
/// </summary>
public class PatientCardDto
{
    /// <summary>
    /// 患者ID（住院号）
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// 性别
    /// </summary>
    public string Gender { get; set; } = null!;
    
    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }
    
    /// <summary>
    /// 床号
    /// </summary>
    public string BedId { get; set; } = null!;
    
    /// <summary>
    /// 护理级别
    /// </summary>
    public NursingGrade NursingGrade { get; set; }
    
    /// <summary>
    /// 患者状态
    /// </summary>
    public PatientStatus Status { get; set; }
    
    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusDisplay { get; set; } = null!;
    
    /// <summary>
    /// 科室
    /// </summary>
    public string Department { get; set; } = null!;
    
    /// <summary>
    /// 病区
    /// </summary>
    public string Ward { get; set; } = null!;
}

/// <summary>
/// 患者完整信息DTO - 用于详情展示和编辑
/// </summary>
public class PatientFullInfoDto
{
    // ==================== 基本信息（不可修改） ====================
    
    /// <summary>
    /// 患者ID（住院号）- 不可修改
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// 姓名 - 不可修改
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// 身份证号 - 不可修改
    /// </summary>
    public string IdCard { get; set; } = null!;
    
    // ==================== 可修改字段 ====================
    
    /// <summary>
    /// 性别
    /// </summary>
    public string Gender { get; set; } = null!;
    
    /// <summary>
    /// 出生日期
    /// </summary>
    public DateTime DateOfBirth { get; set; }
    
    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }
    
    /// <summary>
    /// 身高（cm）
    /// </summary>
    public float Height { get; set; }
    
    /// <summary>
    /// 体重（kg）
    /// </summary>
    public float Weight { get; set; }
    
    /// <summary>
    /// 电话号码
    /// </summary>
    public string PhoneNumber { get; set; } = null!;
    
    /// <summary>
    /// 门诊诊断
    /// </summary>
    public string? OutpatientDiagnosis { get; set; }
    
    /// <summary>
    /// 预约入院时间
    /// </summary>
    public DateTime? ScheduledAdmissionTime { get; set; }
    
    /// <summary>
    /// 实际入院时间
    /// </summary>
    public DateTime? ActualAdmissionTime { get; set; }
    
    /// <summary>
    /// 护理级别
    /// </summary>
    public NursingGrade NursingGrade { get; set; }
    
    // ==================== 关联信息（只读） ====================
    
    /// <summary>
    /// 床号
    /// </summary>
    public string BedId { get; set; } = null!;
    
    /// <summary>
    /// 科室
    /// </summary>
    public string Department { get; set; } = null!;
    
    /// <summary>
    /// 病区
    /// </summary>
    public string Ward { get; set; } = null!;
    
    /// <summary>
    /// 主治医生姓名
    /// </summary>
    public string AttendingDoctorName { get; set; } = null!;
    
    /// <summary>
    /// 患者状态
    /// </summary>
    public PatientStatus Status { get; set; }
}

/// <summary>
/// 患者信息更新请求DTO
/// </summary>
public class UpdatePatientInfoRequest
{
    /// <summary>
    /// 患者ID（必填）
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    // ==================== 可更新字段（前端只传修改的字段） ====================
    
    /// <summary>
    /// 身高（cm）
    /// </summary>
    public float? Height { get; set; }
    
    /// <summary>
    /// 体重（kg）
    /// </summary>
    public float? Weight { get; set; }
    
    /// <summary>
    /// 电话号码
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// 门诊诊断
    /// </summary>
    public string? OutpatientDiagnosis { get; set; }
    
    /// <summary>
    /// 预约入院时间
    /// </summary>
    public DateTime? ScheduledAdmissionTime { get; set; }
    
    /// <summary>
    /// 实际入院时间
    /// </summary>
    public DateTime? ActualAdmissionTime { get; set; }
    
    /// <summary>
    /// 护理级别
    /// </summary>
    public NursingGrade? NursingGrade { get; set; }
    
    // ==================== 操作员信息 ====================
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    public string OperatorId { get; set; } = null!;
    
    /// <summary>
    /// 操作人类型（Doctor/Nurse/Admin）
    /// </summary>
    public string OperatorType { get; set; } = null!;
}

/// <summary>
/// 出院前检查结果DTO
/// </summary>
public class PatientDischargeCheckDto
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 是否可以出院
    /// </summary>
    public bool CanDischarge { get; set; }
    
    /// <summary>
    /// 提示消息
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// 未完成任务列表
    /// </summary>
    public List<UnfinishedTaskDto>? UnfinishedTasks { get; set; }
}

/// <summary>
/// 未完成任务DTO
/// </summary>
public class UnfinishedTaskDto
{
    /// <summary>
    /// 医嘱ID
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// 医嘱类型
    /// </summary>
    public string OrderType { get; set; } = null!;
    
    /// <summary>
    /// 医嘱摘要
    /// </summary>
    public string OrderSummary { get; set; } = null!;
    
    /// <summary>
    /// 医嘱状态
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// 医嘱状态显示文本
    /// </summary>
    public string StatusDisplay { get; set; } = null!;
    
    /// <summary>
    /// 未完成任务数
    /// </summary>
    public int UnfinishedTaskCount { get; set; }
    
    /// <summary>
    /// 最晚任务时间
    /// </summary>
    public DateTime? LatestTaskTime { get; set; }
}

/// <summary>
/// 办理出院请求DTO
/// </summary>
public class ProcessDischargeRequest
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = null!;
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    public string OperatorId { get; set; } = null!;
    
    /// <summary>
    /// 操作人类型
    /// </summary>
    public string OperatorType { get; set; } = null!;
    
    /// <summary>
    /// 出院备注
    /// </summary>
    public string? Remarks { get; set; }
}
