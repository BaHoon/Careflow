namespace CareFlow.Application.DTOs.Nursing
{
    /// <summary>
    /// 创建补充护理任务的DTO
    /// </summary>
    public class CreateSupplementNursingTaskDto
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>
        /// 分配给的护士ID
        /// </summary>
        public string AssignedNurseId { get; set; } = string.Empty;

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; } = "护士自行补充";
    }
}
