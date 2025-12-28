using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; 
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums; 
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CareFlow.Application.Services.Nursing
{
    public class VitalSignService : IVitalSignService
    {
        private readonly ICareFlowDbContext _context;

        public VitalSignService(ICareFlowDbContext context)
        {
            _context = context;
        }

        // --- 静态配置：生命体征正常范围 ---
        // 格式：[指标] = (最小值, 最大值, 异常描述)
        private static readonly Dictionary<string, (decimal Min, decimal Max, string Desc)> NormalRanges = new()
        {
            { "Temperature", (36.0m, 37.3m, "体温异常") },
            { "SysBp",       (90m,   140m,  "收缩压异常") },
            { "DiaBp",       (60m,   90m,   "舒张压异常") },
            { "Pulse",       (60m,   100m,  "脉搏异常") },
            { "Spo2",        (95m,   100m,  "血氧异常") }
        };

        public async Task SubmitVitalSignsAsync(NursingTaskSubmissionDto input)
        {
            Console.WriteLine($"🔍 VitalSignService 收到数据:");
            Console.WriteLine($"  TaskId: {input.TaskId}");
            Console.WriteLine($"  CurrentNurseId: {input.CurrentNurseId}");
            Console.WriteLine($"  ExecutionTime (原始): {input.ExecutionTime} (Kind: {input.ExecutionTime.Kind})");
            Console.WriteLine($"  Temperature: {input.Temperature}");
            Console.WriteLine($"  Pulse: {input.Pulse}");
            
            // 1. 获取原任务
            var task = await _context.Set<NursingTask>().FindAsync(input.TaskId);
            if (task == null) throw new Exception($"未找到ID为 {input.TaskId} 的护理任务");

            // 2. 处理时间：前端传来的是浏览器本地时间（中国时间），需要转换为UTC
            // 如果 Kind 是 Unspecified，假定为中国时间
            DateTime executionTimeUtc;
            if (input.ExecutionTime.Kind == DateTimeKind.Utc)
            {
                executionTimeUtc = input.ExecutionTime;
            }
            else
            {
                // 假定为中国时间 (UTC+8)
                var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var executionTimeChinaUnspecified = DateTime.SpecifyKind(input.ExecutionTime, DateTimeKind.Unspecified);
                executionTimeUtc = TimeZoneInfo.ConvertTimeToUtc(executionTimeChinaUnspecified, chinaTimeZone);
            }
            
            Console.WriteLine($"  转换后UTC时间: {executionTimeUtc} (Kind: {executionTimeUtc.Kind})");

            // 3. 保存体征记录 (VitalSignsRecord - 必填项)
            var vitalRecord = new VitalSignsRecord
            {
                PatientId = task.PatientId,
                RecorderNurseId = input.CurrentNurseId, // 记录是谁测的
                RecordTime = executionTimeUtc,  // 使用转换后的UTC时间
                
                // 【核心】双向关联：记录关联了任务
                NursingTaskId = task.Id, 
                
                Temperature = input.Temperature,
                TempType = input.TempType,
                Pulse = input.Pulse,
                Respiration = input.Respiration,
                SysBp = input.SysBp,
                DiaBp = input.DiaBp,
                Spo2 = input.Spo2,
                PainScore = input.PainScore,
                Weight = input.Weight ?? 0,
                Intervention = input.Intervention ?? string.Empty
            };
            
            await _context.Set<VitalSignsRecord>().AddAsync(vitalRecord);

            // 3. 保存护理笔记 (NursingCareNote - 可选项)
            // 只要有任何一个字段有值，就创建护理笔记记录
            bool hasNursingNote = !string.IsNullOrWhiteSpace(input.NoteContent) 
                || !string.IsNullOrWhiteSpace(input.HealthEducation)
                || !string.IsNullOrWhiteSpace(input.Consciousness)
                || !string.IsNullOrWhiteSpace(input.PipeCareData)
                || input.IntakeVolume.HasValue
                || input.OutputVolume.HasValue;

            if (hasNursingNote)
            {
                var note = new NursingCareNote
                {
                    PatientId = task.PatientId,
                    RecorderNurseId = input.CurrentNurseId,
                    RecordTime = executionTimeUtc,  // 使用转换后的UTC时间
                    
                    // 【核心】关联同一个任务
                    NursingTaskId = task.Id, 
                    
                    // 观察数据
                    Consciousness = input.Consciousness ?? "清醒",
                    PupilLeft = input.PupilLeft ?? "3.0mm/灵敏",
                    PupilRight = input.PupilRight ?? "3.0mm/灵敏",
                    SkinCondition = input.SkinCondition ?? "完好",
                    
                    // 管道护理
                    PipeCareData = input.PipeCareData ?? "{}",
                    
                    // 出入量
                    IntakeVolume = input.IntakeVolume ?? 0,
                    IntakeType = input.IntakeType ?? string.Empty,
                    OutputVolume = input.OutputVolume ?? 0,
                    OutputType = input.OutputType ?? string.Empty,
                    
                    // 护理内容
                    Content = input.NoteContent ?? string.Empty,
                    HealthEducation = input.HealthEducation ?? string.Empty
                };
                await _context.Set<NursingCareNote>().AddAsync(note);
            }

            // 4. 更新任务状态
            task.Status = ExecutionTaskStatus.Completed;
            task.ExecuteTime = executionTimeUtc;  // 使用转换后的UTC时间
            task.ExecutorNurseId = input.CurrentNurseId; // 记录实际执行人（可能和分配的人不一样）

            // 5. 【核心逻辑】智能复测检测
            // 传入刚才生成的 vitalRecord 进行检查
            await CheckAndTriggerReMeasureAsync(vitalRecord, task);

            // 6. 提交事务 (一次性保存所有更改)
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 检查体征数值，如果异常则自动生成复测任务
        /// </summary>
        private async Task CheckAndTriggerReMeasureAsync(VitalSignsRecord vital, NursingTask originalTask)
        {
            var reasons = new List<string>();

            // 逐个指标检查
            CheckRange("Temperature", vital.Temperature, reasons);
            CheckRange("SysBp", vital.SysBp, reasons);
            CheckRange("DiaBp", vital.DiaBp, reasons);
            CheckRange("Pulse", vital.Pulse, reasons);
            CheckRange("Spo2", vital.Spo2, reasons);

            // 如果发现任何异常，生成复测任务
            if (reasons.Any())
            {
                string reasonDesc = string.Join(";", reasons);
                
                var reTask = new NursingTask
                {
                    PatientId = originalTask.PatientId,
                    
                    // 规则：30分钟后复测
                    // ScheduledTime = DateTime.Now.AddMinutes(30), 
                    ScheduledTime = DateTime.SpecifyKind(vital.RecordTime.AddMinutes(30), DateTimeKind.Utc),

                    // 规则：复测任务通常默认分配给原来的护士
                    AssignedNurseId = originalTask.AssignedNurseId, 
                    
                    Status = ExecutionTaskStatus.Pending,
                    TaskType = "ReMeasure", // 标记为复测任务
                    Description = $"{reasonDesc} - 请复测",
                    
                };

                await _context.Set<NursingTask>().AddAsync(reTask);
            }
        }

        // 辅助检查方法
        private void CheckRange(string key, decimal value, List<string> reasons)
        {
            if (NormalRanges.TryGetValue(key, out var rule))
            {
                if (value < rule.Min || value > rule.Max)
                {
                    reasons.Add($"{rule.Desc}({value})");
                }
            }
        }

        /// <summary>
        /// 取消护理任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="nurseId">操作护士ID</param>
        /// <param name="cancelReason">取消理由</param>
        public async Task CancelNursingTaskAsync(long taskId, string nurseId, string cancelReason)
        {
            Console.WriteLine($"📝 VitalSignService.CancelNursingTaskAsync - TaskId: {taskId}, NurseId: {nurseId}, Reason: {cancelReason}");
            
            var task = await _context.Set<NursingTask>().FindAsync(taskId);
            if (task == null)
            {
                Console.WriteLine($"❌ 未找到任务 {taskId}");
                throw new Exception($"未找到ID为 {taskId} 的护理任务");
            }

            Console.WriteLine($"📌 任务当前状态: {task.Status}");
            
            // 只有待执行的任务才能取消
            if (task.Status != ExecutionTaskStatus.Pending)
            {
                Console.WriteLine($"❌ 任务状态不是Pending，无法取消");
                throw new Exception($"任务状态为 {task.Status}，无法取消");
            }

            // 更新任务状态为已取消
            task.Status = ExecutionTaskStatus.Incomplete;
            task.ExecuteTime = DateTime.UtcNow; // 记录取消时间
            task.ExecutorNurseId = nurseId; // 记录取消操作的护士
            task.CancelReason = cancelReason; // 记录取消理由

            Console.WriteLine($"✅ 准备保存，任务状态更新为 Cancelled");
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ 保存成功");
        }

        /// <summary>
        /// 添加护理记录补充说明
        /// </summary>
        public async Task<SupplementDto> AddSupplementAsync(AddSupplementDto dto)
        {
            Console.WriteLine($"📝 添加补充说明 - TaskId: {dto.NursingTaskId}, NurseId: {dto.SupplementNurseId}");
            
            // 验证任务是否存在且已完成
            var task = await _context.Set<NursingTask>().FindAsync(dto.NursingTaskId);
            if (task == null)
            {
                throw new Exception($"未找到ID为 {dto.NursingTaskId} 的护理任务");
            }
            
            if (task.Status != ExecutionTaskStatus.Completed)
            {
                throw new Exception("只能对已完成的护理记录添加补充说明");
            }
            
            // 创建补充记录
            var supplement = new NursingRecordSupplement
            {
                NursingTaskId = dto.NursingTaskId,
                SupplementNurseId = dto.SupplementNurseId,
                SupplementTime = DateTime.UtcNow,
                Content = dto.Content,
                SupplementType = dto.SupplementType
            };
            
            await _context.Set<NursingRecordSupplement>().AddAsync(supplement);
            await _context.SaveChangesAsync();
            
            // 获取护士姓名
            var nurse = await _context.Set<Nurse>().FindAsync(dto.SupplementNurseId);
            
            Console.WriteLine($"✅ 补充说明保存成功 - ID: {supplement.Id}");
            
            return new SupplementDto
            {
                Id = supplement.Id,
                NursingTaskId = supplement.NursingTaskId,
                SupplementNurseId = supplement.SupplementNurseId,
                SupplementNurseName = nurse?.Name ?? "未知",
                SupplementTime = supplement.SupplementTime,
                Content = supplement.Content,
                SupplementType = supplement.SupplementType
            };
        }

        /// <summary>
        /// 获取护理记录的补充说明列表
        /// </summary>
        public async Task<List<SupplementDto>> GetSupplementsAsync(long nursingTaskId)
        {
            var supplements = await _context.Set<NursingRecordSupplement>()
                .Where(s => s.NursingTaskId == nursingTaskId)
                .OrderBy(s => s.SupplementTime)
                .ToListAsync();
            
            var result = new List<SupplementDto>();
            
            foreach (var supplement in supplements)
            {
                var nurse = await _context.Set<Nurse>().FindAsync(supplement.SupplementNurseId);
                
                result.Add(new SupplementDto
                {
                    Id = supplement.Id,
                    NursingTaskId = supplement.NursingTaskId,
                    SupplementNurseId = supplement.SupplementNurseId,
                    SupplementNurseName = nurse?.Name ?? "未知",
                    SupplementTime = supplement.SupplementTime,
                    Content = supplement.Content,
                    SupplementType = supplement.SupplementType
                });
            }
            
            return result;
        }

        /// <summary>
        /// 上传护理记录（自动生成实时护理任务）
        /// 根据完成的护理记录自动生成一个实时的护理任务
        /// 预计时间和实际时间一致，负责护士和实际护士一致
        /// </summary>
        public async Task<long> UploadNursingRecordAsync(long nursingTaskId, string nurseId)
        {
            // 1. 获取原护理任务
            var originalTask = await _context.Set<NursingTask>()
                .Include(t => t.Patient)
                .FirstOrDefaultAsync(t => t.Id == nursingTaskId);
            
            if (originalTask == null)
                throw new Exception($"未找到ID为 {nursingTaskId} 的护理任务");

            // 2. 验证护士存在
            var nurse = await _context.Set<Nurse>().FindAsync(nurseId);
            if (nurse == null)
                throw new Exception($"护士ID {nurseId} 不存在");

            // 3. 创建新的实时护理任务
            var newTask = new NursingTask
            {
                PatientId = originalTask.PatientId,
                AssignedNurseId = nurseId,        // 负责护士为上传人
                ScheduledTime = DateTime.UtcNow,  // 计划时间为当前时间（实时任务）
                Status = ExecutionTaskStatus.Pending,
                TaskType = "RealTime",              // 标记为实时任务
                Description = "护理记录上传自动生成",
                CreateTime = DateTime.UtcNow        // 使用 EntityBase 定义的属性
            };

            // 4. 保存新任务
            _context.Set<NursingTask>().Add(newTask);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ 成功生成实时护理任务 ID: {newTask.Id}，关联患者: {originalTask.PatientId}，负责护士: {nurseId}");

            return newTask.Id;
        }
    }
}