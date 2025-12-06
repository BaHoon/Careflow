using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Nursing;

public class VitalSignsRecord : EntityBase<long>
{
    public string PatientId { get; set; }= null!;
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    public string RecorderNurseId { get; set; }=    null!;
    [ForeignKey("RecorderNurseId")]
    public Nurse RecorderNurse { get; set; } = null!;

    public DateTime RecordTime { get; set; }

    // 身体数据
    public decimal Temperature { get; set; }
    public string TempType { get; set; } = "腋温"; // 腋温/口温
    public int Pulse { get; set; }
    public int Respiration { get; set; }
    public int SysBp { get; set; } // 收缩压
    public int DiaBp { get; set; } // 舒张压
    public decimal Spo2 { get; set; }
    public int PainScore { get; set; } // 0-10
    public decimal Weight { get; set; }
    public string Intervention { get; set; } = string.Empty; // 处置措施
}