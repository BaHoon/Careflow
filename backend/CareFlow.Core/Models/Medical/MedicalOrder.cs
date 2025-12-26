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
        
        //计划校对护士（计算出的签收护士）
        public string? NurseId { get; set; }
        [ForeignKey("NurseId")]
        public Nurse? Nurse { get; set; }
        
        //基础字段
        public DateTime PlantEndTime { get; set; } // 计划结束时间
        public DateTime? EndTime { get; set; } // 实际结束时间
        public string OrderType { get; set; } = null!; // 鉴别列
        public OrderStatus Status { get; set; } = OrderStatus.PendingReceive;
        public bool IsLongTerm { get; set; }

        /// <summary>
        /// 医嘱备注/嘱托
        /// </summary>
        public string? Remarks { get; set; }

        // ==================== 审计字段：签收相关 ====================
        
        /// <summary>
        /// 实际签收护士ID（实际执行签收操作的护士，可能与计划NurseId不同）
        /// </summary>
        public string? SignedByNurseId { get; set; }
        [ForeignKey("SignedByNurseId")]
        public Nurse? SignedByNurse { get; set; }
        
        /// <summary>
        /// 护士签收时间（Accepted状态时记录）
        /// </summary>
        public DateTime? SignedAt { get; set; }

        // ==================== 审计字段：退回相关 (Rejected状态) ====================
        
        /// <summary>
        /// 退回原因（护士退回医嘱时填写）
        /// </summary>
        public string? RejectReason { get; set; }
        
        /// <summary>
        /// 退回时间
        /// </summary>
        public DateTime? RejectedAt { get; set; }
        
        /// <summary>
        /// 退回护士ID（执行退回操作的护士）
        /// </summary>
        public string? RejectedByNurseId { get; set; }
        [ForeignKey("RejectedByNurseId")]
        public Nurse? RejectedByNurse { get; set; }

        // ==================== 审计字段：停嘱相关 (PendingStop/Stopped状态) ====================
        
        /// <summary>
        /// 停嘱原因（医生下达停嘱时填写）
        /// </summary>
        public string? StopReason { get; set; }
        
        /// <summary>
        /// 医生下达停嘱的时间（进入PendingStop状态的时间）
        /// </summary>
        public DateTime? StopOrderTime { get; set; }
        
        /// <summary>
        /// 下达停嘱的医生ID（可能与创建医嘱的医生不同）
        /// </summary>
        public string? StopDoctorId { get; set; }
        [ForeignKey("StopDoctorId")]
        public Doctor? StopDoctor { get; set; }
        
        /// <summary>
        /// 护士确认停嘱的时间（进入Stopped状态的时间）
        /// </summary>
        public DateTime? StopConfirmedAt { get; set; }
        
        /// <summary>
        /// 确认停嘱的护士ID
        /// </summary>
        public string? StopConfirmedByNurseId { get; set; }
        [ForeignKey("StopConfirmedByNurseId")]
        public Nurse? StopConfirmedByNurse { get; set; }
        
        /// <summary>
        /// 护士拒绝停嘱的原因（如果护士拒绝停嘱）
        /// </summary>
        public string? StopRejectReason { get; set; }
        
        /// <summary>
        /// 护士拒绝停嘱的时间
        /// </summary>
        public DateTime? StopRejectedAt { get; set; }
        
        /// <summary>
        /// 拒绝停嘱的护士ID
        /// </summary>
        public string? StopRejectedByNurseId { get; set; }
        [ForeignKey("StopRejectedByNurseId")]
        public Nurse? StopRejectedByNurse { get; set; }

        // ==================== 审计字段：撤销相关 (Cancelled状态) ====================
        
        /// <summary>
        /// 医生撤销医嘱的原因
        /// </summary>
        public string? CancelReason { get; set; }
        
        /// <summary>
        /// 撤销时间
        /// </summary>
        public DateTime? CancelledAt { get; set; }
        
        /// <summary>
        /// 撤销医嘱的医生ID（可能与创建医嘱的医生不同）
        /// </summary>
        public string? CancelledByDoctorId { get; set; }
        [ForeignKey("CancelledByDoctorId")]
        public Doctor? CancelledByDoctor { get; set; }

        // ==================== 审计字段：完成相关 (Completed状态) ====================
        
        /// <summary>
        /// 医嘱完成时间（进入Completed状态的时间）
        /// 注意：与PlantEndTime（计划结束时间）和EndTime（实际结束时间）不同
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// 完成类型（Normal=正常到期, Early=提前终止, Abnormal=异常终止）
        /// </summary>
        public string? CompletionType { get; set; }

        // ==================== 审计字段：重新提交相关 (Rejected → PendingReceive) ====================
        
        /// <summary>
        /// 最后一次重新提交时间（医生修改后重新提交）
        /// </summary>
        public DateTime? ResubmittedAt { get; set; }
        
        /// <summary>
        /// 修改说明（医生在重新提交时填写的修改内容）
        /// </summary>
        public string? ModificationNotes { get; set; }

        // ==================== 导航属性：状态变更历史 ====================
        
        /// <summary>
        /// 状态变更历史记录（导航属性，指向独立的历史表）
        /// </summary>
        public ICollection<MedicalOrderStatusHistory> StatusHistories { get; set; } = new List<MedicalOrderStatusHistory>();

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
        
        /// <summary>
        /// 开始/执行时间
        /// - IMMEDIATE: 不使用（系统自动使用当前时间）
        /// - SPECIFIC:  唯一的执行时刻（仅执行一次）
        /// - CYCLIC:    首次执行时间（后续按 IntervalHours 递增）
        /// - SLOTS:     起始日期（与 SmartSlotsMask 结合使用）
        /// </summary>
        public DateTime? StartTime { get; set; }     
        // public DateTime? EndTime { get; set; }    // 基类已有 EndTime，这里注释掉避免冲突，或者使用 new 覆盖
        
        public string TimingStrategy { get; set; } = null!;  // 策略类型(IMMEDIATE/SPECIFIC/CYCLIC/SLOTS)
        
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
        public string ItemName { get; set; } = null!;         // 检查项目名称（如"血常规"、"头颅CT"等）
        public string RisLisId { get; set; } = null!;         // 申请单号
        public string Location { get; set; } = null!;         // 检查科室位置
        public InspectionSource Source { get; set; }          // 检查来源 (RIS/LIS)
        
        public DateTime? AppointmentTime { get; set; }        // 预约时间
        public string? AppointmentPlace { get; set; }         // 预约地点
        public string? Precautions { get; set; }              // 注意事项（如空腹、憋尿等）
        
        // --- 闭环时间节点 ---
        public DateTime? CheckStartTime { get; set; }         // 检查开始时间
        public DateTime? CheckEndTime { get; set; }           // 检查结束时间
        public DateTime? ReportPendingTime { get; set; }      // 报告待出时间
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
        public string SurgeonId { get; set; } = null!;        // 主刀医生ID
        
        // 建议在DbContext中配置为 jsonb 类型

        public string? RequiredTalk { get; set; }             // "需进行的宣讲JSON"
        public string? RequiredOperation { get; set; }        // "需进行的操作JSON"
        // 注意：需带入药品使用基类的 Items 导航属性 (MedicationOrderItem 集合)
        
        public float PrepProgress { get; set; }      // 术前准备进度(0.0-1.0)
        public string PrepStatus { get; set; } = null!;       // 准备状态
    }

    // 出院医嘱 (DISCHARGE_ORDER)
    [Table("DischargeOrders")]
    public class DischargeOrder : MedicalOrder
    {
        /// <summary>
        /// 出院类型（治愈出院、好转出院、转院、死亡等）
        /// </summary>
        public DischargeType DischargeType { get; set; }
        
        /// <summary>
        /// 出院时间
        /// </summary>
        public DateTime DischargeTime { get; set; }
        
        /// <summary>
        /// 出院诊断
        /// </summary>
        public string DischargeDiagnosis { get; set; } = null!;
        
        /// <summary>
        /// 出院医嘱
        /// </summary>
        public string DischargeInstructions { get; set; } = string.Empty;
        
        /// <summary>
        /// 出院带药说明
        /// </summary>
        public string MedicationInstructions { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否需要随访
        /// </summary>
        public bool RequiresFollowUp { get; set; }
        
        /// <summary>
        /// 随访时间
        /// </summary>
        public DateTime? FollowUpDate { get; set; }
        
        /// <summary>
        /// 出院确认护士ID
        /// </summary>
        public string? DischargeConfirmedByNurseId { get; set; }
        
        /// <summary>
        /// 出院确认时间
        /// </summary>
        public DateTime? DischargeConfirmedAt { get; set; }
        
        // 注意：Items 集合已在 MedicalOrder 基类中定义，用于存储出院带药清单
    }

    // ==================== 医嘱状态变更历史表 ====================
    
    /// <summary>
    /// 医嘱状态变更历史记录表
    /// 用于完整的审计追踪，记录医嘱状态的每一次变更
    /// </summary>
    [Table("MedicalOrderStatusHistories")]
    public class MedicalOrderStatusHistory : EntityBase<long>
    {
        /// <summary>
        /// 关联的医嘱ID
        /// </summary>
        public long MedicalOrderId { get; set; }
        [ForeignKey("MedicalOrderId")]
        public MedicalOrder MedicalOrder { get; set; } = null!;
        
        /// <summary>
        /// 变更前的状态
        /// </summary>
        public OrderStatus FromStatus { get; set; }
        
        /// <summary>
        /// 变更后的状态
        /// </summary>
        public OrderStatus ToStatus { get; set; }
        
        /// <summary>
        /// 状态变更时间
        /// </summary>
        public DateTime ChangedAt { get; set; }
        
        /// <summary>
        /// 操作人ID（可能是医生ID或护士ID）
        /// </summary>
        public string ChangedById { get; set; } = null!;
        
        /// <summary>
        /// 操作人类型（Doctor/Nurse/System）
        /// </summary>
        public string ChangedByType { get; set; } = null!;
        
        /// <summary>
        /// 操作人姓名（冗余字段，便于查询）
        /// </summary>
        public string? ChangedByName { get; set; }
        
        /// <summary>
        /// 变更原因/说明
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// 备注信息
        /// </summary>
        public string? Notes { get; set; }
    }
}