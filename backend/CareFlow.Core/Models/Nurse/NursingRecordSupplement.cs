using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Nursing;

/// <summary>
/// 护理记录补充说明
/// </summary>
public class NursingRecordSupplement : EntityBase<long>
{
    // 关联的护理任务ID
    public long NursingTaskId { get; set; }
    [ForeignKey("NursingTaskId")]
    public NursingTask NursingTask { get; set; } = null!;
    
    // 补充护士
    public string SupplementNurseId { get; set; } = null!;
    [ForeignKey("SupplementNurseId")]
    public Nurse SupplementNurse { get; set; } = null!;
    
    // 补充时间
    public DateTime SupplementTime { get; set; }
    
    // 补充内容
    public string Content { get; set; } = string.Empty;
    
    // 补充类型：Correction(更正), Addition(补充)
    public string SupplementType { get; set; } = "Addition";
}
