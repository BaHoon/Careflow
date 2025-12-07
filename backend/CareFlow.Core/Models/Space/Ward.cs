using CareFlow.Core.Models.Organization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models;

namespace CareFlow.Core.Models.Space
{
    public class Ward : EntityBase<string>
    {
        public string DepartmentId { get; set; } = null!;
        
        [ForeignKey("DepartmentId")]
        public Department Department { get; set; } = null!;
        
        public ICollection<Bed> Beds { get; set; } = null!;
    }
}