using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Application.Services.Nursing
{
    public class NursingTaskGenerator
    {
        private readonly ICareFlowDbContext _context;
        private readonly INurseAssignmentService _nurseAssignmentService; // 注入排班服务

        // 构造函数注入 DbContext 和 排班服务
        public NursingTaskGenerator(
            ICareFlowDbContext context, 
            INurseAssignmentService nurseAssignmentService)
        {
            _context = context;
            _nurseAssignmentService = nurseAssignmentService;
        }

        /// <summary>
        /// 核心方法：为指定科室的所有患者生成今日护理任务
        /// </summary>
        public async Task GenerateDailyTasksAsync(string departmentId, DateOnly date)
        {
            // 1. 获取该科室所有在院患者
            // Include Bed 和 Ward 是为了确保患者位置信息存在（虽然排班服务里可能也会查，但这里预加载比较稳妥）
            var patients = await _context.Set<Patient>()
                .Include(p => p.Bed)
                .ThenInclude(b => b.Ward)
                .Where(p => p.Bed.Ward.DepartmentId == departmentId && p.Status == "Active")
                .ToListAsync();

            var newTasks = new List<NursingTask>();

            foreach (var patient in patients)
            {
                // 2. 根据护理等级获取需要执行的时间点列表
                // 注意：数据库里存的是 int，这里强转为枚举方便处理
                var timeSlots = GetTimeSlotsByGrade((NursingGrade)patient.NursingGrade);

                foreach (var time in timeSlots)
                {
                    // 3. 构造计划时间 (Date + TimeSpan)
                    var dt = date.ToDateTime(TimeOnly.FromTimeSpan(time));
                    var plannedTime = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                    // 4. 【核心集成】调用你的排班服务，计算该时间点、该患者的责任护士
                    // 这一步实现了“任务生成即分配”
                    string? assignedNurseId = await _nurseAssignmentService.CalculateResponsibleNurseAsync(patient.Id, plannedTime);

                    // 5. 幂等性检查：防止重复生成
                    // 检查是否已经存在同一病人、同一时间、类型为常规护理的任务
                    bool exists = await _context.Set<NursingTask>().AnyAsync(t => 
                        t.PatientId == patient.Id && 
                        t.TaskType == "Routine" && 
                        t.ScheduledTime == plannedTime);

                    if (exists) continue;

                    // 6. 创建新的轻量级护理任务 (NursingTask)
                    var task = new NursingTask
                    {
                        PatientId = patient.Id,
                        ScheduledTime = plannedTime,
                        
                        // 填入计算出的护士ID (如果没排班，这里可能是null，允许后续人工分配)
                        AssignedNurseId = assignedNurseId, 
                        
                        Status = "Pending",
                        TaskType = "Routine", // 标记为常规任务
                        Description = $"常规护理 (等级:{(NursingGrade)patient.NursingGrade})",
                        
                    };

                    newTasks.Add(task);
                }
            }

            // 7. 批量保存
            if (newTasks.Any())
            {
                await _context.Set<NursingTask>().AddRangeAsync(newTasks);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 辅助方法：根据护理等级返回时间点
        /// </summary>
        private List<TimeSpan> GetTimeSlotsByGrade(NursingGrade grade)
        {
            return grade switch
            {
                // 三级护理: 每日1次 (14:00)
                NursingGrade.Grade3 => new List<TimeSpan> { 
                    new(6, 0, 0) 
                },

                // 二级护理: 每日2次 (08:00, 16:00)
                NursingGrade.Grade2 => new List<TimeSpan> { 
                    new(0, 0, 0), 
                    new(8, 0, 0) 
                },

                // 一级护理: 每日3次 (08:00, 16:00, 20:00)
                NursingGrade.Grade1 => new List<TimeSpan> { 
                    new(0, 0, 0), 
                    new(8, 0, 0),
                    new(12, 0, 0)
                },

                // 特级护理: 每2小时一次 (08:00, 10:00 ... )
                NursingGrade.Special => new List<TimeSpan> { 
                    new(8, 0, 0), new(10, 0, 0), new(12, 0, 0), new(14, 0, 0),
                    new(16, 0, 0), new(18, 0, 0), new(20, 0, 0), new(22, 0, 0),
                    new(0, 0, 0), new(2, 0, 0), new(4, 0, 0), new(6, 0, 0)
                },

                _ => new List<TimeSpan>() // 默认不生成
            };
        }
    }
}