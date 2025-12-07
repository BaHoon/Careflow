using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;

namespace CareFlow.Core.Models.Nursing;

// 班次定义 (白班、夜班)
public class ShiftType : EntityBase<string>
{
    public string ShiftName { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

// 护士排班表
public class NurseRoster : EntityBase<long>
{
    public string StaffId { get; set; } = null!;
    [ForeignKey("StaffId")]
    public Nurse Nurse { get; set; } = null!;

    public string WardId { get; set; } = null!;
    [ForeignKey("WardId")]
    public Ward Ward { get; set; } = null!;

    public string ShiftId { get; set; } = null!;
    [ForeignKey("ShiftId")]
    public ShiftType ShiftType { get; set; } = null!;

    public DateOnly WorkDate { get; set; } // 使用 DateOnly 更精准
    public string Status { get; set; } = "Scheduled"; // Scheduled, CheckedIn, Leave
}