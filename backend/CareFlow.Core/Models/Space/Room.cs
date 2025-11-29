using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class Room : SoftDeleteEntity<int>
{
    public int DeptId { get; set; } // 所属科室
    public string Building { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomGenderType GenderConstraint { get; set; } // 性别限制
    public RoomType Type { get; set; }
    
    // 关系
    [ForeignKey("DeptId")]
    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
}