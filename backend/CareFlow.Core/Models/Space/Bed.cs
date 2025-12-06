using System.ComponentModel.DataAnnotations;
namespace CareFlow.Core.Models.Space
{
    public class Bed
    {
        [Key]
        public string BedId { get; set; } = null!;
        public string WardId { get; set; } = null!;
        public Ward Ward { get; set; } = null!;
        public string Status { get; set; } = null!; // 占用/空闲
    }
}