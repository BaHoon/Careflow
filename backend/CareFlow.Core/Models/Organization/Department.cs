using System.ComponentModel.DataAnnotations;
using CareFlow.Core.Models.Space;
namespace CareFlow.Core.Models.Organization
{
    public class Department
    {
        [Key]
        public string DeptId { get; set; }= null!;
        public string DeptName { get; set; } = null!;
        public string Location { get; set; } = null!;
        
        public ICollection<Staff> StaffList { get; set; } = null!;
        public ICollection<Ward> Wards { get; set; } = null!;
    }
}