using CareFlow.Application.Interfaces; // 引用基础DbContext接口或具体Context
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using CareFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Repositories
{
    public class NurseScheduleRepository : EfRepository<NurseRoster, long>, INurseScheduleRepository
    {
        // 构造函数注入 DbContext
        public NurseScheduleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<NurseRoster?> GetNurseOnDutyAsync(string wardId, DateTime currentTime)
        {
            // 重要：currentTime 应该是 UTC 时间，与数据库中的 WorkDate（UTC日期）保持一致
            // 1. 获取当天的日期 (DateOnly，UTC) 和 当前时刻 (TimeSpan)
            var today = DateOnly.FromDateTime(currentTime);
            var timeNow = currentTime.TimeOfDay;

            // 2. 先把当天该病区所有的排班查出来
            // 注意：我们需要 Include 班次信息(ShiftType)来比较时间，Include Nurse 来获取护士详情
            var potentialRosters = await _context.Set<NurseRoster>()
                .Include(r => r.ShiftType)
                .Include(r => r.Nurse)
                .Where(r => 
                    r.WardId == wardId && 
                    r.WorkDate == today && 
                    r.Status == "Scheduled") // 只查已排班状态
                .ToListAsync();

            // 3. 在内存中过滤具体的时间 (比在SQL中处理TimeSpan比较更稳定)
            // 我们需要找到一个班次，使得 timeNow 在 StartTime 和 EndTime 之间
            var match = potentialRosters.FirstOrDefault(r => 
                IsTimeInShift(timeNow, r.ShiftType.StartTime, r.ShiftType.EndTime)
            );

            return match;
        }

        /// <summary>
        /// 辅助方法：判断时间是否在班次范围内
        /// </summary>
        private bool IsTimeInShift(TimeSpan now, TimeSpan start, TimeSpan end)
        {
            // 情况A：白班 (例如 08:00 - 16:00) -> Start < End
            if (start <= end)
            {
                return now >= start && now <= end;
            }
            // 情况B：夜班跨天 (例如 22:00 - 06:00) -> Start > End
            // 只要当前时间大于开始时间 (23:00) 或者 小于结束时间 (05:00) 都算在班
            else
            {
                return now >= start || now <= end;
            }
        }
    }
}