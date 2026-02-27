namespace CareFlow.Application.DTOs.Nursing
{
    public class NursingTaskSubmissionDto
    {
        public long TaskId { get; set; }       // 必须传 NursingTask 的 ID
        public string CurrentNurseId { get; set; } = string.Empty; // 当前操作护士
        public DateTime ExecutionTime { get; set; }

        // --- 体征数据 (VitalSignsRecord - 必填项) ---
        public decimal Temperature { get; set; }
        public string TempType { get; set; } = "腋温";
        public int Pulse { get; set; }
        public int Respiration { get; set; }
        public int SysBp { get; set; }
        public int DiaBp { get; set; }
        public decimal Spo2 { get; set; }
        public int PainScore { get; set; }
        public decimal? Weight { get; set; }
        public string? Intervention { get; set; }
        
        // --- 护理笔记 (NursingCareNote - 可选项) ---
        // 观察数据
        public string? Consciousness { get; set; } // 意识状态
        public string? PupilLeft { get; set; }     // 左瞳孔
        public string? PupilRight { get; set; }    // 右瞳孔
        public string? SkinCondition { get; set; } // 皮肤状况
        
        // 管道护理
        public string? PipeCareData { get; set; }  // 管道数据 JSON
        
        // 出入量
        public decimal? IntakeVolume { get; set; }  // 入量
        public string? IntakeType { get; set; }     // 入量类型
        public decimal? OutputVolume { get; set; }  // 出量
        public string? OutputType { get; set; }     // 出量类型
        
        // 护理内容
        public string? NoteContent { get; set; }      // 病情观察
        public string? HealthEducation { get; set; }  // 健康教育
    }
}