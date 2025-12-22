namespace CareFlow.Core.Enums;

public enum DrugCategory { Oral = 1, Injection = 2, External = 3, Material = 4, Exam = 5 }
public enum OrderType { LongTerm = 1, Temporary = 2 } // 长期/临时

/// <summary>
/// 医嘱状态
/// </summary>
public enum OrderStatus 
{ 
    /// <summary>未提交</summary>
    Draft = 0,
    /// <summary>待护士签收</summary>
    PendingReceive = 1,
    /// <summary>已签收</summary>
    Accepted = 2, 
    /// <summary>执行中</summary>
    InProgress = 3,
    /// <summary>已完成</summary>
    Completed = 4,
    /// <summary>已停止</summary>
    Stopped = 5, 
    /// <summary>已取消</summary>
    Cancelled = 6 
}

public enum TaskType { Dispensing = 1, Administration = 2, Patrol = 3 } // 配药/给药/巡视

/// <summary>
/// 执行任务状态枚举
/// </summary>
public enum ExecutionTaskStatus 
{ 
    /// <summary>待申请 - 任务刚生成，等待护士提交申请（仅药品/检查任务）</summary>
    Applying = 0,
    
    /// <summary>已申请 - 护士已提交申请，等待外部接口反馈</summary>
    Applied = 1,
    
    /// <summary>申请已确认 - 外部接口反馈完毕（药房已配药/检查站已排号）</summary>
    AppliedConfirmed = 2,
    
    /// <summary>待执行 - 无需申请的任务直接进入此状态，或申请确认后进入</summary>
    Pending = 3,
    
    /// <summary>执行中 - 护士正在执行任务</summary>
    InProgress = 4,
    
    /// <summary>已完成 - 任务执行完毕</summary>
    Completed = 5,
    
    /// <summary>已取消 - 医生取消了医嘱</summary>
    Cancelled = 6,
    
    /// <summary>已撤回 - 护士主动撤回申请（仅限Applied状态可撤回）</summary>
    Revoked = 7,
    
    /// <summary>已跳过 - 护士跳过此次执行</summary>
    Skipped = 8
}

/// <summary>
/// 医嘱时间执行策略
/// </summary>
public enum TimingStrategy
{
    /// <summary>
    /// 立即执行 (临时医嘱，单次执行)
    /// </summary>
    Immediate = 1,
    
    /// <summary>
    /// 指定时间执行 (单次，如指定在某天某时刻执行)
    /// </summary>
    Specific = 2,
    
    /// <summary>
    /// 周期性执行 (按固定时间间隔，如每隔6小时)
    /// </summary>
    Cyclic = 3,
    
    /// <summary>
    /// 时段执行 (按医院时间槽位，如早餐前、午餐后等，通过SmartSlotsMask指定)
    /// </summary>
    Slots = 4
}