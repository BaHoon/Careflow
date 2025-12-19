namespace CareFlow.Application.DTOs.Nursing
{
    public class NursingTaskSubmissionDto
    {
        public long TaskId { get; set; }       // 必须传 NursingTask 的 ID
        public string CurrentNurseId { get; set; } = string.Empty; // 当前操作护士
        public DateTime ExecutionTime { get; set; }

        // --- 体征数据 ---
        public decimal Temperature { get; set; }
        public string TempType { get; set; } = "腋温";
        public int Pulse { get; set; }
        public int Respiration { get; set; }
        public int SysBp { get; set; }
        public int DiaBp { get; set; }
        public decimal Spo2 { get; set; }
        public int PainScore { get; set; }
        
        // --- 护理笔记 (可选) ---
        // 如果前端填了字，就有值；没填就是 null
        public string? NoteContent { get; set; } 
        public string? PipeCareData { get; set; } // 管道数据 JSON
    }
}