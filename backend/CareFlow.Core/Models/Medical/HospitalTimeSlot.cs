using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareFlow.Core.Models.Medical
{
    // 对应 HOSPITAL_TIME_SLOT
    public class HospitalTimeSlot : EntityBase<int>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // 手动指定ID (1, 2, 4, 8...)
        public string SlotCode { get; set; } = null!;        // 代码(PRE_BREAKFAST)
        public string SlotName { get; set; } = null!;   // 名称(早餐前)
        public TimeSpan DefaultTime { get; set; }    // 默认时间(07:00)
        public int OffsetMinutes { get; set; }       // 偏差
    }
}