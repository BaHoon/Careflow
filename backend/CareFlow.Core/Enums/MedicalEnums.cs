namespace CareFlow.Core.Enums;

public enum DrugCategory { Oral = 1, Injection = 2, External = 3, Material = 4, Exam = 5 }
public enum OrderType { LongTerm = 1, Temporary = 2 } // 长期/临时
public enum OrderStatus { Draft = 0, PendingReview = 1, Accepted = 2, Stopped = 3, Cancelled = 4 }
public enum TaskType { Dispensing = 1, Administration = 2, Patrol = 3 } // 配药/给药/巡视
public enum ExecutionTaskStatus { Pending = 1, Dispensed = 2, InProgress = 3, Completed = 4, Cancelled = 5 }

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