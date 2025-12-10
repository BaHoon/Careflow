using System;
using System.Collections.Generic;
using System.Text.Json;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Enums; 

namespace CareFlow.Application.Services
{
    public class SurgicalExecutionTaskFactory : IExecutionTaskFactory
    {
        // 内部辅助类：用于解析药品 JSON
        private class MedItemDto
        {
            public string DrugId { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        // 定义各阶段相对于手术排期的时间偏移量
        private static readonly TimeSpan TalkOffset = TimeSpan.FromHours(-16);  // 术前宣教：前16小时
        private static readonly TimeSpan OpOffset = TimeSpan.FromHours(-2);     // 术前操作：前2小时
        private static readonly TimeSpan SupplyOffset = TimeSpan.FromHours(-1); // 物品核对：前1小时

        public List<ExecutionTask> CreateTasks(CareFlow.Core.Models.Medical.MedicalOrder medicalOrder)
        {
            // 1. 类型安全检查
            if (medicalOrder is not SurgicalOrder surgicalOrder)
            {
                throw new ArgumentException("SurgicalExecutionTaskFactory 仅支持 SurgicalOrder 类型的医嘱", nameof(medicalOrder));
            }

            var tasks = new List<ExecutionTask>();

            // 2. 业务逻辑：拆解宣教 (JSON数组 -> 多个任务)
            GenerateTalkTasks(surgicalOrder, tasks);

            // 3. 业务逻辑：拆解操作 (JSON数组 -> 多个任务)
            GenerateOperationTasks(surgicalOrder, tasks);

            // 4. 业务逻辑：聚合药品与器械 (JSON数组 + 默认值 -> 单个任务)
            GenerateSupplyTask(surgicalOrder, tasks);

            return tasks;
        }

        private void GenerateTalkTasks(SurgicalOrder order, List<ExecutionTask> tasks)
        {
            if (string.IsNullOrWhiteSpace(order.RequiredTalk)) return;

            try
            {
                var talkItems = JsonSerializer.Deserialize<List<string>>(order.RequiredTalk);
                if (talkItems == null) return;

                foreach (var talkContent in talkItems)
                {
                    tasks.Add(new ExecutionTask
                    {
                        MedicalOrderId = order.Id,
                        PatientId = order.PatientId,
                        // 导航属性通常在Save时由EF自动处理，这里主要保证ID正确
                        
                        PlannedStartTime = order.ScheduleTime.Add(TalkOffset),
                        Status = "Pending", // 或使用 ExecutionTaskStatus.Pending.ToString()
                        CreatedAt = DateTime.UtcNow,
                        
                        DataPayload = JsonSerializer.Serialize(new
                        {
                            TaskType = "EDUCATION",
                            Title = talkContent,
                            Description = $"针对手术【{order.SurgeryName}】的术前宣教",
                            IsChecklist = false
                        })
                    });
                }
            }
            catch
            {
                // 解析失败时不阻断流程，可选择记录日志或跳过
            }
        }

        private void GenerateOperationTasks(SurgicalOrder order, List<ExecutionTask> tasks)
        {
            if (string.IsNullOrWhiteSpace(order.RequiredOperation)) return;

            try
            {
                var opItems = JsonSerializer.Deserialize<List<string>>(order.RequiredOperation);
                if (opItems == null) return;

                foreach (var opContent in opItems)
                {
                    tasks.Add(new ExecutionTask
                    {
                        MedicalOrderId = order.Id,
                        PatientId = order.PatientId,
                        PlannedStartTime = order.ScheduleTime.Add(OpOffset),
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        
                        DataPayload = JsonSerializer.Serialize(new
                        {
                            TaskType = "NURSING_OP",
                            Title = opContent,
                            Description = $"切口部位：{order.IncisionSite}",
                            RequiresScan = true // 标记前端需要扫码
                        })
                    });
                }
            }
            catch { /* Ignore */ }
        }

        private void GenerateSupplyTask(SurgicalOrder order, List<ExecutionTask> tasks)
        {
            var supplyList = new List<object>();

            // A. 基础器械包 (默认都有)
            supplyList.Add(new { Name = $"{order.SurgeryName} 基础器械包", Count = "1套", Type = "Equipment" });

            // B. 药品 JSON 解析
            if (!string.IsNullOrWhiteSpace(order.RequiredMeds))
            {
                try
                {
                    var meds = JsonSerializer.Deserialize<List<MedItemDto>>(order.RequiredMeds);
                    if (meds != null)
                    {
                        foreach (var med in meds)
                        {
                            // 注意：这里暂时只存DrugId，实际显示时前端需关联或后端Service层注入DrugService查询
                            supplyList.Add(new { Name = med.DrugId, Count = $"{med.Count}", Type = "Drug" });
                        }
                    }
                }
                catch
                {
                    supplyList.Add(new { Name = "药品数据解析异常", Count = "N/A", Type = "Error" });
                }
            }

            // 生成唯一的聚合任务
            tasks.Add(new ExecutionTask
            {
                MedicalOrderId = order.Id,
                PatientId = order.PatientId,
                PlannedStartTime = order.ScheduleTime.Add(SupplyOffset),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                
                DataPayload = JsonSerializer.Serialize(new
                {
                    TaskType = "SUPPLY_CHECK",
                    Title = "术前物品与药品核对",
                    Description = "请核对带入手术室的所有物品",
                    IsChecklist = true,
                    Items = supplyList
                })
            });
        }
    }
}