using System.ComponentModel.DataAnnotations.Schema;

namespace CareFlow.Core.Models;

public class OrderItem : EntityBase<long> // 注意这里用 long
{
    public Guid OrderId { get; set; }
    public int DrugId { get; set; }
    public decimal Dosage { get; set; }
    public string DosageUnit { get; set; } = string.Empty;
    public int GroupSequence { get; set; } // 混药分组

    public virtual MedicalOrder Order { get; set; } = null!;
    public virtual Medication Medication { get; set; } = null!;
}