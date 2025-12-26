namespace CareFlow.Core.Enums
{
    public enum TaskCategory
    {
        // 1. 即刻执行类 (如：单次给药、更换引流袋) -> 扫码即完成
        Immediate = 1,

        // 2. 持续执行类 (如：输液、吸氧) -> 扫码开始，再次扫码结束
        Duration = 2,

        // 3. 结果等待类 (如：皮试、血糖) -> 扫码开始，录入结果后结束
        ResultPending = 3,

        // 4. 护理记录类 (如：体温单) -> 扫码患者，录入数据提交
        DataCollection = 4,

        // 5. 核对类 (如：取药) -> 扫描任务，逐个扫描药品码后结束
        Verification = 5,

        // 6. 申请打印类 (如：检查申请) -> 提交申请，打印导引单后结束
        ApplicationWithPrint = 6,

        // 7. 出院确认类 (如：出院医嘱确认) -> 扫码患者，确认出院相关事项后完成
        DischargeConfirmation = 11
    }
}