using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models.Medical
{
    // 对应 MEDICAL_ORDER (基类)
    public abstract class MedicalOrder : EntityBase<long>
    {
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
        public DateTime PlantEndTime { get; set; } // 理论结束
        public DateTime? EndTime { get; set; } // 实际结束
        public string OrderType { get; set; } = null!; // 鉴别列
        public string Status { get; set; } = null!;
        public bool IsLongTerm { get; set; }

        /// <summary>
        /// 医嘱备注/嘱托
        /// </summary>
        public string? Remarks { get; set; }

        // [新增] 包含的药品列表 (例如：500ml盐水 + 0.5mg青霉素)
        // 移动到基类，以便手术医嘱等也能使用
        public ICollection<MedicationOrderItem> Items { get; set; } = new List<MedicationOrderItem>();
    }

    // --- 新增：医嘱药品明细表 (用于解决多药混合问题) ---
    [Table("MedicationOrderItems")]
    public class MedicationOrderItem : EntityBase<long>
    {
        public long MedicalOrderId { get; set; }
        [ForeignKey("MedicalOrderId")] // 这是一个导航属性，指向父医嘱
        public MedicalOrder MedicalOrder { get; set; } = null!;

        public string DrugId { get; set; } = null!;
        [ForeignKey("DrugId")]
        public Drug Drug { get; set; } = null!;

        public string Dosage { get; set; } = null!; // 该药品的单次剂量 (如 0.5g)
        
        // 某些特殊情况，不同药品的单位可能不同，这里仅作记录
        public string Note { get; set; } = string.Empty; 
    }

    // 药品医嘱 (MEDICATION_ORDER)
    [Table("MedicationOrders")]
    public class MedicationOrder : MedicalOrder
    {
        // public string DrugId { get; set; } = null!;           // 药品ID
        // [ForeignKey("DrugId")]
        // public Drug Drug { get; set; } = null!;               // 药品信息
        
        // public string Dosage { get; set; } = null!;           // 剂量

        public UsageRoute UsageRoute { get; set; }               // 用法途径
        public bool IsDynamicUsage { get; set; }     // 是否不定量(如吸氧)
        
        /// <summary>
        /// 执行间隔（小时数）- 仅用于 CYCLIC 策略
        /// 例如：6 表示每6小时执行一次，24 表示每天一次
        /// 支持小数：0.5 表示每30分钟一次
        /// null 或 0 表示不适用（IMMEDIATE/SPECIFIC/SLOTS策略）
        /// </summary>
        public decimal? IntervalHours { get; set; }
        
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
        public InspectionSource Source { get; set; }          // 检查来源 (RIS/LIS)
        
        public DateTime? AppointmentTime { get; set; }        // 预约时间
        public string? AppointmentPlace { get; set; }         // 预约地点
        public string? Precautions { get; set; }              // 注意事项（如空腹、憋尿等）
        
        // --- 闭环时间节点 ---
        public DateTime? CheckStartTime { get; set; }         // 检查开始时间
        public DateTime? CheckEndTime { get; set; }           // 检查结束时间
        public DateTime? BackToWardTime { get; set; }         // 返回病房时间
        public DateTime? ReportTime { get; set; }             // 报告完成时间
        public string? ReportId { get; set; }                 // 报告编号(冗余或关联)
        
        public InspectionOrderStatus InspectionStatus { get; set; } = InspectionOrderStatus.Pending; // 检查状态
        
        // 导航属性
        public ICollection<InspectionReport> Reports { get; set; } = new List<InspectionReport>();
    }

    // 手术医嘱 (SURGICAL_ORDER)
    [Table("SurgicalOrders")]
    public class SurgicalOrder : MedicalOrder
    {
        public string SurgeryName { get; set; } = null!;      // 手术名称
        public DateTime ScheduleTime { get; set; }            // 排期时间
        public string AnesthesiaType { get; set; } = null!;   // 麻醉方式
        public string IncisionSite { get; set; } = null!;     // 切口部位
        
        // 建议在DbContext中配置为 jsonb 类型

        public string? RequiredTalk { get; set; }             // "需进行的宣讲JSON"
        public string? RequiredOperation { get; set; }        // "需进行的操作JSON"
        public string? RequiredMeds { get; set; } = null!;        // 需带入药品JSON
        
        public float PrepProgress { get; set; }      // 术前准备进度(0.0-1.0)
        public string PrepStatus { get; set; } = null!;       // 准备状态
    }
}