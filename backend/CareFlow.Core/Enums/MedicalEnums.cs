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
    Cancelled = 6,
    /// <summary>已退回</summary>
    Rejected=7,
    /// <summary>待签收停止医嘱</summary>
    PendingStop=8,
    /// <summary>停止中 - 护士已签收停止医嘱，停止节点之前的任务仍在执行</summary>
    StoppingInProgress=9
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
    
    /// <summary>停止医嘱锁定 - 医嘱下达停止指令时锁定任务</summary>
    OrderStopping = 6,
    
    /// <summary>已停止/作废 - 医嘱停止或撤销导致的任务作废</summary>
    Stopped = 7,
    
    /// <summary>异常/拒绝 - 护士跳过执行或执行异常</summary>
    Incomplete = 8,
    
    /// <summary>待退药 - 医嘱停止后需要护士确认退药，或护士主动申请退药</summary>
    PendingReturn = 9
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

/// <summary>
/// 出院类型枚举
/// </summary>
public enum DischargeType
{
    /// <summary>治愈出院</summary>
    Cured = 1,
    
    /// <summary>好转出院</summary>
    Improved = 2,
    
    /// <summary>转院</summary>
    Transfer = 3,
    
    /// <summary>自动出院</summary>
    AutoDischarge = 4,
    
    /// <summary>死亡</summary>
    Death = 5,
    
    /// <summary>其他</summary>
    Other = 99
}