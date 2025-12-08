using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models.Medical
{
    // 对应 INSPECTION_REPORT
    public class InspectionReport : EntityBase<long>
    {
        // 关联检查医嘱
        public long OrderId { get; set; }
        [ForeignKey("OrderId")]
        public InspectionOrder InspectionOrder { get; set; } = null!;

        // 冗余关联患者方便查询
        public string PatientId { get; set; } = null!;
        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = null!;

        public string RisLisId { get; set; } = null!;                 // 外部系统ID (RIS/LIS报告编号)
        public DateTime ReportTime { get; set; }                      // 报告时间
        public InspectionReportStatus ReportStatus { get; set; }      // 状态(待出/已出/已审核)
        
        public string? Findings { get; set; }                         // 检查所见/情况描述
        public string? Impression { get; set; }                       // 诊断意见/诊断性总结
        
        public string? AttachmentUrl { get; set; }                    // 影像PDF/图片链接
        public string? ReviewerId { get; set; }                       // 审核医生ID
        [ForeignKey("ReviewerId")]
        public Doctor? Reviewer { get; set; }                         // 审核医生
        
        public InspectionSource ReportSource { get; set; }            // 数据来源 (RIS/LIS)
    }
}