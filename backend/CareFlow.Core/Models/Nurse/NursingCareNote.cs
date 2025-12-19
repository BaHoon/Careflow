using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Nursing;

public class NursingCareNote : EntityBase<long>
{
    // [新增] 关联的护理任务ID (不再关联 ExecutionTask)
    public long? NursingTaskId { get; set; }
    public string PatientId { get; set; }= null!;
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    public string RecorderNurseId { get; set; }= null!; 
    [ForeignKey("RecorderNurseId")]
    public Nurse RecorderNurse { get; set; } = null!;
    public DateTime RecordTime { get; set; }

    // 观察数据
    public string Consciousness { get; set; } = "清醒";
    public string PupilLeft { get; set; } = "3.0mm/灵敏";
    public string PupilRight { get; set; } = "3.0mm/灵敏";
    public string SkinCondition { get; set; } = "完好";
    
    // 管道护理 (JSON 存储)
    [Column(TypeName = "jsonb")]
    public string PipeCareData { get; set; } = "{}"; 

    // 出入量
    public decimal IntakeVolume { get; set; }
    public string IntakeType { get; set; } = string.Empty;
    public decimal OutputVolume { get; set; }
    public string OutputType { get; set; } = string.Empty;

    // 内容
    public string Content { get; set; } = string.Empty; // 病情观察
    public string HealthEducation { get; set; } = string.Empty; // 健康教育

    public bool IsAmended { get; set; }
    public long? ParentAmendId { get; set; }
}