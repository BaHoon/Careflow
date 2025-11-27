using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

// 定义过敏史快照的类 (用于存JSON)
public class AllergySnapshot { public List<string> Allergens { get; set; } = new(); }

public class Admission : SoftDeleteEntity<Guid>
{
    public Guid PatientId { get; set; }
    public Guid AttendingDoctorId { get; set; } // 主治医
    public Guid ResidentDoctorId { get; set; }  // 住院医
    public int CurrentDeptId { get; set; }
    public int CurrentBedId { get; set; }

    public DateTime AdmissionTime { get; set; }
    public DateTime? DischargeTime { get; set; }
    public CareLevel CareLevel { get; set; }
    public AdmissionStatus Status { get; set; }
    
    // JSON 字段：过敏史快照
    public AllergySnapshot? AllergySnapshot { get; set; } 

    [Column(TypeName = "decimal(18,2)")]
    public decimal AccountBalance { get; set; }

    // 关系
    public virtual PatientArchive Patient { get; set; } = null!;
    public virtual Doctor AttendingDoctor { get; set; } = null!;
    public virtual Doctor ResidentDoctor { get; set; } = null!;
    public virtual Department CurrentDept { get; set; } = null!;
    public virtual Bed CurrentBed { get; set; } = null!;
}