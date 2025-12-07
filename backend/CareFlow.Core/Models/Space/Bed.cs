using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models;

namespace CareFlow.Core.Models.Space
{
    public class Bed : EntityBase<string>
    {
        public string WardId { get; set; } = null!;
        
        [ForeignKey("WardId")]
        public Ward Ward { get; set; } = null!;
        
        public string Status { get; set; } = null!; // 占用/空闲
    }
}