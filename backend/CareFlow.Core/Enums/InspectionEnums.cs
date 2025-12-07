namespace CareFlow.Core.Enums;

/// <summary>
/// 检查医嘱状态
/// </summary>
public enum InspectionOrderStatus
{
    /// <summary>
    /// 待前往 - 医嘱已开立，等待患者前往检查
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// 检查中 - 患者正在进行检查
    /// </summary>
    InProgress = 2,
    
    /// <summary>
    /// 已回病房 - 检查完成，患者已返回病房
    /// </summary>
    BackToWard = 3,
    
    /// <summary>
    /// 报告已出 - 检查报告已完成
    /// </summary>
    ReportCompleted = 4,
    
    /// <summary>
    /// 已取消 - 检查医嘱被取消
    /// </summary>
    Cancelled = 5
}

/// <summary>
/// 检查报告状态
/// </summary>
public enum InspectionReportStatus
{
    /// <summary>
    /// 待出 - 报告尚未完成
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// 已出 - 报告已完成，可查看
    /// </summary>
    Completed = 2
}

/// <summary>
/// 检查项目来源系统
/// </summary>
public enum InspectionSource
{
    /// <summary>
    /// RIS - 放射信息系统（如CT、核磁等影像检查）
    /// </summary>
    RIS = 1,
    
    /// <summary>
    /// LIS - 实验室信息系统（如抽血、尿检等化验）
    /// </summary>
    LIS = 2
}
