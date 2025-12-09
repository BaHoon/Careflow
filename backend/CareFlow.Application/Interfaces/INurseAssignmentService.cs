namespace CareFlow.Application.Interfaces
{
    public interface INurseAssignmentService
    {
        /// <summary>
        /// 根据患者ID和开医嘱时间，计算出负责该医嘱的护士ID
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="orderTime">开立时间</param>
        /// <returns>护士ID (如果没找到值班护士，可能返回 null)</returns>
        Task<string?> CalculateResponsibleNurseAsync(string patientId, DateTime orderTime);
    }
}