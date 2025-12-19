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
            // 1. 获取原任务
            var task = await _context.Set<NursingTask>().FindAsync(input.TaskId);
            if (task == null) throw new Exception($"未找到ID为 {input.TaskId} 的护理任务");

            // 2. 保存体征记录 (VitalSignsRecord)
            var vitalRecord = new VitalSignsRecord
            {
                PatientId = task.PatientId,
                RecorderNurseId = input.CurrentNurseId, // 记录是谁测的
                RecordTime = input.ExecutionTime,
                
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
                // Weight 等其他字段如果DTO里有可以加上
            };
            
            await _context.Set<VitalSignsRecord>().AddAsync(vitalRecord);

            // 3. 保存护理笔记 (NursingCareNote) - 仅当有内容时
            if (!string.IsNullOrWhiteSpace(input.NoteContent) || !string.IsNullOrEmpty(input.PipeCareData))
            {
                var note = new NursingCareNote
                {
                    PatientId = task.PatientId,
                    RecorderNurseId = input.CurrentNurseId,
                    RecordTime = input.ExecutionTime,
                    
                    // 【核心】关联同一个任务，这样以后查这个任务就能同时看到体征和笔记
                    NursingTaskId = task.Id, 
                    
                    Content = input.NoteContent ?? "",
                    PipeCareData = input.PipeCareData ?? "{}"
                };
                await _context.Set<NursingCareNote>().AddAsync(note);
            }

            // 4. 更新任务状态
            task.Status = "Completed";
            task.ExecuteTime = input.ExecutionTime;
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
                    
                    Status = "Pending",
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