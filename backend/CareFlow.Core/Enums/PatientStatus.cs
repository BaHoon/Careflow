namespace CareFlow.Core.Enums
{
    /// <summary>
    /// 患者状态枚举
    /// </summary>
    public enum PatientStatus
    {
        /// <summary>
        /// 待入院
        /// </summary>
        PendingAdmission = 0,

        /// <summary>
        /// 在院
        /// </summary>
        Hospitalized = 1,

        /// <summary>
        /// 待出院
        /// </summary>
        PendingDischarge = 2,

        /// <summary>
        /// 已出院
        /// </summary>
        Discharged = 3
    }
}
