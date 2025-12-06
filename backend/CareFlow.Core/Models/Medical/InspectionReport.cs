using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Medical
{
    // 对应 INSPECTION_REPORT
    public class InspectionReport
    {
        [Key]
        public long ReportId { get; set; }           // 报告ID

        // 关联检查医嘱
        public long OrderId { get; set; }
            [ForeignKey("OrderId")]
        public InspectionOrder InspectionOrder { get; set; } = null!;

        // 冗余关联患者方便查询
        public string PatientId { get; set; } = null!;
        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = null!;

        public string RisLisId { get; set; } = null!;         // 外部系统ID
        public DateTime ReportTime { get; set; }
        public string ReportStatus { get; set; } = null!;     // 状态(待出/已出)
        
        public string Findings { get; set; } = null!;         // 检查所见
        public string Impression { get; set; } = null!;      // 诊断意见
        
        public string AttachmentUrl { get; set; } = null!;    // 影像链接
        public string ReviewerId { get; set; } = null!;       // 审核医生
        public string ReportSource { get; set; } = null!;    // 数据来源
    }
}