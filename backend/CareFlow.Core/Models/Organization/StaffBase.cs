using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

// 员工表：使用 Guid 作为主键
public class StaffBase : SoftDeleteEntity<Guid>
{
    [Required]
    [MaxLength(20)]
    public string EmployeeNumber { get; set; } = string.Empty; // 工号

    [Required]
    public string PasswordHash { get; set; } = string.Empty; // 密码
    
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;
    
    public StaffType StaffType { get; set; } // 引用刚才写的枚举
    
    public bool IsActive { get; set; } = true;

    // --- 外键关系 ---
    public int DeptId { get; set; } // 外键ID
    
    [ForeignKey("DeptId")]
    public virtual Department Department { get; set; } = null!; // 对应的科室对象
}