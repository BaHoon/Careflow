using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models; // 引用 EntityBase 所在的命名空间

namespace CareFlow.Core.Models.Organization
{
    // 1. 继承 EntityBase<string>，意味着这个类自动拥有了一个 string 类型的 Id
    public class Staff : EntityBase<string>
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string DeptCode { get; set; } = string.Empty;
        [ForeignKey("DeptCode")]
        public Department Department { get; set; } = null!;
    }

    [Table("Doctors")]
    public class Doctor : Staff
    {
        public string Title { get; set; } = string.Empty;
        public string PrescriptionAuthLevel { get; set; } = string.Empty;
    }

    [Table("Nurses")]
    public class Nurse : Staff
    {
        public string NurseRank { get; set; } = string.Empty;
    }
}