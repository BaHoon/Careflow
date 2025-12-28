using System.ComponentModel;
namespace CareFlow.Core.Enums
{
    /// <summary>
    /// 给药途径
    /// </summary>
    public enum UsageRoute
    {
        // --- 1. 口服/外用类 (对应 Immediate 即刻任务) ---
        [Description("口服")]
        PO = 1,       // Per Os
        
        [Description("外用/涂抹")]
        Topical = 2, // Topical Application
        
        [Description("肌内注射")]
        IM = 10,      // Intramuscular (通常打完就结束，算即刻)
        
        [Description("皮下注射")]
        SC = 11,      // Subcutaneous (如胰岛素，打完结束)
        
        [Description("静脉推注")]
        IVP = 12,     // IV Push (推完结束)

        // --- 2. 持续输注类 (对应 Duration 持续任务) ---
        [Description("静脉滴注")]
        IVGTT = 20,   // Intravenous Guttal (吊瓶，需拔针)
        
        // [Description("吸氧")]
        // Inhalation = 21, // 雾化/吸氧

        // --- 3. 结果观察类 (对应 ResultPending 结果任务) ---
        [Description("皮试")]
        ST = 30       // Skin Test (明确的皮试)
    }
}