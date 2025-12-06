using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareFlow.Core.Models.Organization
{
    // 对应 STAFF 表
    public class Staff
    {
        [Key]
        public string StaffId { get; set; }= string.Empty;// 工号
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty; // 建议后续改为 Enum
        public bool IsActive { get; set; }

        public string DeptCode { get; set; } = string.Empty;
        [ForeignKey("DeptCode")]
        public Department Department { get; set; } = null!;
    }

    // 对应 DOCTOR 表 (继承 Staff)
    [Table("Doctors")]
    public class Doctor : Staff
    {
        public string Title { get; set; } = string.Empty; // 职称
        public string PrescriptionAuthLevel { get; set; } = string.Empty;
    }

    // 对应 NURSE 表 (继承 Staff)
    [Table("Nurses")]
    public class Nurse : Staff
    {
        public string NurseRank { get; set; } = string.Empty;
    }
}