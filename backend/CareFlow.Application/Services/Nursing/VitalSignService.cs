using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; 
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Enums; 
using Microsoft.EntityFrameworkCore;

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
    }
}