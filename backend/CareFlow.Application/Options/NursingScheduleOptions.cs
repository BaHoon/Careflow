namespace CareFlow.Application.Options;

/// <summary>
/// 护理任务调度配置选项
/// 定义所有调度参数
/// </summary>
public class NursingScheduleOptions
{
    public const string SectionName = "NursingTaskScheduling";

    /// <summary>
    /// 每日任务生成配置
    /// </summary>
    public DailyTaskGenerationOptions DailyTaskGeneration { get; set; } = new();

    /// <summary>
    /// 交班配置
    /// </summary>
    public ShiftHandoverOptions ShiftHandover { get; set; } = new();

    /// <summary>
    /// 逾期提醒配置
    /// </summary>
    public OverdueReminderOptions OverdueReminder { get; set; } = new();

    /// <summary>
    /// 时区配置
    /// </summary>
    public string TimeZoneId { get; set; } = "China Standard Time";
}

/// <summary>
/// 每日任务生成配置
/// </summary>
public class DailyTaskGenerationOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 触发时间 (格式: "HH:mm")
    /// </summary>
    public string TriggerTime { get; set; } = "00:00";

    /// <summary>
    /// 任务时段配置 (格式: ["08:00", "12:00", "16:00", "20:00"])
    /// </summary>
    public List<string> TaskTimeSlots { get; set; } = new()
    {
        "08:00",
        "12:00",
        "16:00",
        "20:00"
    };
}

/// <summary>
/// 交班配置
/// </summary>
public class ShiftHandoverOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 交班时间点 (格式: ["08:00", "16:00", "00:00"])
    /// </summary>
    public List<string> ShiftTimes { get; set; } = new()
    {
        "08:00",
        "16:00",
        "00:00"
    };
}

/// <summary>
/// 逾期提醒配置
/// </summary>
public class OverdueReminderOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 检查间隔（分钟）
    /// </summary>
    public int IntervalMinutes { get; set; } = 10;

    /// <summary>
    /// NursingTask 容忍时间配置（分钟）
    /// </summary>
    public Dictionary<string, int> NursingTaskTolerances { get; set; } = new()
    {
        { "Routine", 90 },      // 常规护理：90分钟
        { "ReMeasure", 30 }     // 复测：30分钟
    };

    /// <summary>
    /// ExecutionTask 容忍时间配置（分钟）
    /// 按医嘱类型分类
    /// </summary>
    public Dictionary<string, int> ExecutionTaskTolerances { get; set; } = new()
    {
        { "MedicationOrder_IMMEDIATE", 15 },   // 立即执行的药品：15分钟
        { "MedicationOrder_Default", 30 },     // 其他药品医嘱：30分钟
        { "InspectionOrder", 60 },             // 检查医嘱：60分钟
        { "SurgicalOrder", 120 },              // 手术医嘱：120分钟
        { "OperationOrder", 30 }               // 操作医嘱：30分钟
    };

    /// <summary>
    /// 超过容忍期后，多久算严重延迟（分钟）
    /// </summary>
    public int SevereDelayAfterToleranceMinutes { get; set; } = 30;
}
