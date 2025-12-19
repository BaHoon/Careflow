namespace CareFlow.Application.DTOs.MedicalOrder
{
    // 药品明细 DTO
    public class MedicationItemDto
    {
        public string DrugId { get; set; } = null!;
        public string Dosage { get; set; } = null!;
        public string? Note { get; set; }
    }

    public class CreateOrderRequestDto
    {
        // === 1. 必填基础信息 ===
        public string OrderType { get; set; } = null!; // 关键字段：Surgical, Medication, etc.
        public string PatientId { get; set; } = null!;
        public string DoctorId { get; set; } = null!;
        public bool IsLongTerm { get; set; }
        
        // === 2. 药品医嘱特有字段 (Medication) ===
        public string? DrugId { get; set; }
        public string? Dosage { get; set; }
        public string? UsageRoute { get; set; }
        
        /// <summary>
        /// 执行间隔（小时数），仅用于 CYCLIC 策略
        /// 例如：6 表示每6小时一次，12 表示每12小时一次
        /// 支持小数：0.5 表示每30分钟一次，1.5 表示每90分钟一次
        /// </summary>
        public decimal? IntervalHours { get; set; }
        
        public bool? IsDynamicUsage { get; set; }
        public string? TimingStrategy { get; set; }
        public int? SmartSlotsMask { get; set; }
        public int? IntervalDays { get; set; }

        // === 3. 手术医嘱特有字段 (Surgical) ===
        public string? SurgeryName { get; set; }
        public DateTime? ScheduleTime { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? IncisionSite { get; set; }
        // RequiredMeds 已改为 Items 列表
        public List<MedicationItemDto>? MedicationItems { get; set; }  // 手术需带入的药品
        public string? RequiredTalk { get; set; }      // JSON
        public string? RequiredOperation { get; set; } // JSON

        // ... 如果有检查医嘱字段继续往下加
    }
}