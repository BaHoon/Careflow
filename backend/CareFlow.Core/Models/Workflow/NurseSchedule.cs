using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class NurseSchedule : EntityBase<long>
{
    public Guid NurseId { get; set; }
    public DateTime WorkDate { get; set; }
    public ShiftType ShiftType { get; set; }
    
    public virtual Nurse Nurse { get; set; } = null!;
}