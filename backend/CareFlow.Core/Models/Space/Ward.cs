using CareFlow.Core.Models.Organization;
using System.ComponentModel.DataAnnotations;
namespace CareFlow.Core.Models.Space

{
    public class Ward
    {
        [Key]
        public string WardId { get; set; } = null!;
        public string DeptId { get; set; } = null!;
        public Department Department { get; set; } = null!;
        public ICollection<Bed> Beds { get; set; } = null!;
    }
}