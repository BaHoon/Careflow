namespace CareFlow.Application.DTOs.MedicalOrder
{
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
        public string? FreqCode { get; set; }
        public bool? IsDynamicUsage { get; set; }
        public string? TimingStrategy { get; set; }
        public int? SmartSlotsMask { get; set; }
        public int? IntervalDays { get; set; }

        // === 3. 手术医嘱特有字段 (Surgical) ===
        public string? SurgeryName { get; set; }
        public DateTime? ScheduleTime { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? IncisionSite { get; set; }
        public string? RequiredMeds { get; set; }      // JSON
        public string? RequiredTalk { get; set; }      // JSON
        public string? RequiredOperation { get; set; } // JSON

        // ... 如果有检查医嘱字段继续往下加
    }
}