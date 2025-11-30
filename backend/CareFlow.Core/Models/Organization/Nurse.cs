using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

[Table("Nurses")] // 同样是继承，单独存一张表
public class Nurse : StaffBase
{
    public NurseRank Rank { get; set; } // 职级

    // ER图里的 "Primary_View" (默认视图)，我们可以暂时用 string 或者再建个枚举
    public string PrimaryView { get; set; } = string.Empty; 
}