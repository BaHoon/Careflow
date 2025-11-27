using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class MedicalOrder : SoftDeleteEntity<Guid>
{
    public Guid AdmissionId { get; set; }
    public Guid DoctorId { get; set; }
    
    public OrderType OrderType { get; set; } // 长嘱/临嘱
    public OrderStatus Status { get; set; }
    public bool IsEmergency { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string FrequencyCode { get; set; } = string.Empty; // QD, BID

    public virtual Admission Admission { get; set; } = null!;
    public virtual Doctor Doctor { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}