namespace CareFlow.Application.DTOs.OrderAcknowledgement;

/// <summary>
/// 签收医嘱响应DTO
/// </summary>
public class AcknowledgeOrdersResponseDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// 每条医嘱的签收结果
    /// </summary>
    public List<AcknowledgedOrderResultDto> Results { get; set; } = new();
    
    /// <summary>
    /// 错误列表（如果有）
    /// </summary>
    public List<string>? Errors { get; set; }
}

/// <summary>
/// 单条医嘱签收结果DTO
/// </summary>
public class AcknowledgedOrderResultDto
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
    /// 今天是否需要执行
    /// </summary>
    public bool NeedTodayAction { get; set; }
    
    /// <summary>
    /// 需要的操作类型（RequestMedication/RequestInspection/None）
    /// </summary>
    public string ActionType { get; set; } = "None";
    
    /// <summary>
    /// 生成的任务ID列表
    /// </summary>
    public List<long> GeneratedTaskIds { get; set; } = new();
    
    /// <summary>
    /// 任务统计信息
    /// </summary>
    public TaskGenerationSummary TaskSummary { get; set; } = new();
    
    // === 停止医嘱特有字段 ===
    
    /// <summary>
    /// 是否有待处理的申请（停止医嘱专用）
    /// TODO: 阶段三实现
    /// </summary>
    public bool HasPendingRequests { get; set; }
    
    /// <summary>
    /// 待处理的申请ID列表（停止医嘱专用）
    /// TODO: 阶段三实现
    /// </summary>
    public List<long>? PendingRequestIds { get; set; }
}

/// <summary>
/// 任务生成统计信息
/// </summary>
public class TaskGenerationSummary
{
    /// <summary>
    /// 总任务数
    /// </summary>
    public int TotalTaskCount { get; set; }
    
    /// <summary>
    /// 今日任务数
    /// </summary>
    public int TodayTaskCount { get; set; }
    
    /// <summary>
    /// 已分配责任护士的任务数
    /// </summary>
    public int AssignedTaskCount { get; set; }
    
    /// <summary>
    /// 未分配责任护士的任务数（排班不足）
    /// </summary>
    public int UnassignedTaskCount { get; set; }
}
