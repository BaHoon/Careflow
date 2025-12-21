namespace CareFlow.Core.Enums;

public enum DrugCategory { Oral = 1, Injection = 2, External = 3, Material = 4, Exam = 5 }
public enum OrderType { LongTerm = 1, Temporary = 2 } // 长期/临时

/// <summary>
/// 医嘱状态
/// </summary>
public enum OrderStatus 
{ 
    /// <summary>待护士签收</summary>
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
public enum ExecutionTaskStatus { Applying = 0, Applyend = 1, Pending = 2, InProgress = 3, Completed = 4, Cancelled = 5, Skipped = 6} // 任务状态

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