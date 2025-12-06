using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;

namespace CareFlow.Core.Models.Medical
{
    // 对应 MEDICAL_ORDER (基类)
    public abstract class MedicalOrder
    {
        [Key]
        public long OrderId { get; set; } // 流水号
        
        public string PatientId { get; set; } = null!;
        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = null!;

        public string DoctorId { get; set; } = null!;
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = null!;
        
        //校对护士
        public string? NurseId { get; set; }
        [ForeignKey("NurseId")]
        public Nurse? Nurse { get; set; }
        
        //基础字段
        public DateTime CreateTime { get; set; }
        public DateTime PlantEndTime { get; set; } // 理论结束
        public DateTime? EndTime { get; set; } // 实际结束
        public string OrderType { get; set; } = null!; // 鉴别列
        public string Status { get; set; } = null!;
        public bool IsLongTerm { get; set; }
    }

    // 药品医嘱 (MEDICATION_ORDER)
    [Table("MedicationOrders")]
    public class MedicationOrder : MedicalOrder
    {
        public string DrugId { get; set; } = null!;           // 药品ID
        public string Dosage { get; set; } = null!;           // 剂量
        public string UsageRoute { get; set; } = null!;       // 途径(口服/静滴/涂抹)
        public bool IsDynamicUsage { get; set; }     // 是否不定量(如吸氧)
        
        public string FreqCode { get; set; } = null!;         // 关联频次字典
        
        // ER图中特有的起止时间 (可能与基类重叠，若业务需要独立控制生效期则保留)
        public DateTime? StartTime { get; set; }     
        // public DateTime? EndTime { get; set; }    // 基类已有 EndTime，这里注释掉避免冲突，或者使用 new 覆盖
        
        public string TimingStrategy { get; set; } = null!;  // 策略类型(IMMEDIATE/SPECIFIC/CYCLIC/SLOTS)
        public DateTime? SpecificExecutionTime { get; set; } // 指定执行时间
        
        public int SmartSlotsMask { get; set; }      // 时段位掩码(Bitmask)
        public int IntervalDays { get; set; }        // 间隔天数(1=每天, 2=隔天)
    }

    // 操作医嘱 (OPERATION_ORDER)
    [Table("OperationOrders")]
    public class OperationOrder : MedicalOrder
    {
        public string OpId { get; set; } = null!;             // 操作代码
        public bool Normal { get; set; }             // 正常/异常标识
        public string FrequencyType { get; set; } = null!;    // 频次类型(每天一次/一天几次)
        public string FrequencyValue { get; set; } = null!;   // 频次值
    }


    // 检查医嘱 (INSPECTION_ORDER)
    [Table("InspectionOrders")]
    public class InspectionOrder : MedicalOrder
    {
        public string ItemCode { get; set; } = null!;         // 检查项目代码
        public string RisLisId { get; set; } = null!;         // 申请单号
        public string Location { get; set; } = null!;         // 检查科室位置
        
        public DateTime? AppointmentTime { get; set; } // 预约时间
        public string AppointmentPlace { get; set; } = null!;   // 预约地点
        public string Precautions { get; set; } = null!;        // 注意事项
        
        // --- 闭环时间节点 ---
        public DateTime? CheckStartTime { get; set; }
        public DateTime? CheckEndTime { get; set; }
        public DateTime? BackToWardTime { get; set; }
        public DateTime? ReportTime { get; set; }
        public string ReportId { get; set; } = null!;          // 报告编号(冗余或关联)
    }

    // 3. 手术医嘱 (SURGICAL_ORDER)
    [Table("SurgicalOrders")]
    public class SurgicalOrder : MedicalOrder
    {
        public string SurgeryName { get; set; } = null!;      // 手术名称
        public DateTime ScheduleTime { get; set; }   // 排期时间
        public string AnesthesiaType { get; set; } = null!;   // 麻醉方式
        public string IncisionSite { get; set; } = null!;     // 切口部位
        
        // 建议在DbContext中配置为 jsonb 类型
        public string RequiredMeds { get; set; } = null!;        // 需带入药品JSON
        
        public bool NeedBloodPrep { get; set; }      // 是否备血
        public bool HasImplants { get; set; }        // 有无假体/饰品
        
        public float PrepProgress { get; set; }      // 术前准备进度(0.0-1.0)
        public string PrepStatus { get; set; } = null!;       // 准备状态
    }
}