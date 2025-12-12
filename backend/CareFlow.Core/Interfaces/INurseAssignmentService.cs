using CareFlow.Core.Models.Nursing;

namespace CareFlow.Core.Interfaces
{
    // 继承基础 IRepository 
    public interface INurseScheduleRepository : IRepository<NurseRoster, long>
    {
        /// <summary>
        /// 根据病区ID和当前时间，查找正在值班的护士排班记录
        /// </summary>
        /// <param name="wardId">病区/病房ID</param>
        /// <param name="currentTime">当前时间</param>
        /// <returns>匹配的排班记录(包含Nurse信息)，如果没有则返回null</returns>
        Task<NurseRoster?> GetNurseOnDutyAsync(string wardId, DateTime currentTime);
    }
}