using System.ComponentModel.DataAnnotations;

namespace CareFlow.Core.Models;

// 科室表：使用 int 作为主键，继承 SoftDeleteEntity 表示它可以被软删除
public class Department : SoftDeleteEntity<int>
{
    [Required]
    [MaxLength(50)]
    public string DeptName { get; set; } = string.Empty;

    public bool IsClinical { get; set; } // 是否临床科室

    // --- 关系定义 ---
    // 一个科室有多个员工
    public virtual ICollection<StaffBase> Staffs { get; set; } = new List<StaffBase>();
}