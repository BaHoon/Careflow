using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Nursing;

public class VitalSignsRecord : EntityBase<long>
{
    // [新增] 关联的护理任务ID
    public long? NursingTaskId { get; set; }

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

// [新表] 护理任务单
public class NursingTask : EntityBase<long>
{
    public string PatientId { get; set; } = null!;
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    // --- 1. 计划与分配 ---
    public DateTime ScheduledTime { get; set; } // 计划时间 (08:00, 16:00...)
        
    // [用户需求] 自动分配的责任护士
    // 生成任务时，根据排班表(NurseRoster)填入
    public string? AssignedNurseId { get; set; } 
        [ForeignKey("AssignedNurseId")]
    public Nurse? AssignedNurse { get; set; }

    // --- 2. 执行情况 ---
    public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled
    public DateTime? ExecuteTime { get; set; }      // 实际执行时间
        
    // [用户需求] 实际执行护士 (可能和分配的不一样，比如帮忙代测)
    public string? ExecutorNurseId { get; set; }    

    // --- 3. 任务元数据 ---
    public string TaskType { get; set; } = "Routine"; // Routine(常规), ReMeasure(复测)
    public string? Description { get; set; }          // "高热复测"
}