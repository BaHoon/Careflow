using System.ComponentModel.DataAnnotations;
using CareFlow.Core.Models.Space;
using CareFlow.Core.Models;

namespace CareFlow.Core.Models.Organization
{
    public class Department : EntityBase<string>
    {
        public string DeptName { get; set; } = null!;
        public string Location { get; set; } = null!;
        
        public ICollection<Staff> StaffList { get; set; } = null!;
        public ICollection<Ward> Wards { get; set; } = null!;
    }
}