using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class Bed : SoftDeleteEntity<int>
{
    public int RoomId { get; set; }
    public string BedLabel { get; set; } = string.Empty; // A床
    public BedStatus Status { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerDay { get; set; }

    [ForeignKey("RoomId")]
    public virtual Room Room { get; set; } = null!;
}