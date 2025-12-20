using System;
using System.Collections.Generic;
using System.Text.Json;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Enums;

namespace CareFlow.Application.Services
{
    /// <summary>
    /// 操作类医嘱任务生成工厂
    /// 根据 FrequencyType 和 FrequencyValue 生成执行任务
    /// 根据操作代码（OpId）自动分配 TaskCategory（Immediate/Duration/ResultPending）
    /// </summary>
    public class OperationExecutionTaskFactory : IExecutionTaskFactory
    {
        // 操作代码到操作名称的映射（后续可以改为从数据库读取）
        private static readonly Dictionary<string, string> OperationNameMap = new()
        {
            { "OP001", "更换引流袋" },
            { "OP002", "持续吸氧" },
            { "OP003", "测量生命体征" },
            { "OP004", "翻身拍背" },
            { "OP005", "更换敷料" },
            { "OP006", "导尿" },
            { "OP007", "鼻饲" },
            { "OP008", "雾化吸入" },
            { "OP009", "口腔护理" },
            { "OP010", "会阴护理" },
            { "OP011", "皮肤护理" },
            { "OP012", "协助翻身" },
            { "OP013", "持续心电监护" },
            { "OP014", "持续导尿" },
            { "OP015", "持续胃肠减压" },
            { "OP016", "持续静脉输液" },
            { "OP017", "血糖监测" },
            { "OP018", "血压监测" },
            { "OP019", "体温监测" },
            { "OP020", "尿量监测" },
            { "OP021", "意识状态评估" },
            { "OP022", "疼痛评估" }
        };

        /// <summary>
        /// 根据操作代码（OpId）获取任务类别
        /// 参考药品医嘱的 GetTaskCategoryFromUsageRoute 方法
        /// </summary>
        private TaskCategory GetTaskCategoryFromOpId(string opId)
        {
            return opId switch
            {
                // Immediate 类：即刻执行，扫码即完成
                "OP001" => TaskCategory.Immediate,  // 更换引流袋
                "OP004" => TaskCategory.Immediate,  // 翻身拍背
                "OP005" => TaskCategory.Immediate,  // 更换敷料
                "OP006" => TaskCategory.Immediate,  // 导尿
                "OP009" => TaskCategory.Immediate,  // 口腔护理
                "OP010" => TaskCategory.Immediate,  // 会阴护理
                "OP011" => TaskCategory.Immediate,  // 皮肤护理
                "OP012" => TaskCategory.Immediate,  // 协助翻身
                
                // Duration 类：持续执行，需要开始和结束时间
                "OP002" => TaskCategory.Duration,   // 持续吸氧
                "OP007" => TaskCategory.Duration,   // 鼻饲
                "OP008" => TaskCategory.Duration,   // 雾化吸入
                "OP013" => TaskCategory.Duration,   // 持续心电监护
                "OP014" => TaskCategory.Duration,   // 持续导尿
                "OP015" => TaskCategory.Duration,   // 持续胃肠减压
                "OP016" => TaskCategory.Duration,   // 持续静脉输液
                
                // ResultPending 类：需要等待结果，录入结果后完成
                "OP003" => TaskCategory.ResultPending, // 测量生命体征
                "OP017" => TaskCategory.ResultPending, // 血糖监测
                "OP018" => TaskCategory.ResultPending, // 血压监测
                "OP019" => TaskCategory.ResultPending, // 体温监测
                "OP020" => TaskCategory.ResultPending, // 尿量监测
                "OP021" => TaskCategory.ResultPending, // 意识状态评估
                "OP022" => TaskCategory.ResultPending, // 疼痛评估
                
                // 默认为立即执行
                _ => TaskCategory.Immediate
            };
        }

        public List<ExecutionTask> CreateTasks(CareFlow.Core.Models.Medical.MedicalOrder medicalOrder)
        {
            // 1. 类型安全检查
            if (medicalOrder is not OperationOrder operationOrder)
            {
                throw new ArgumentException("OperationExecutionTaskFactory 仅支持 OperationOrder 类型的医嘱", nameof(medicalOrder));
            }

            // 2. 获取操作的任务类别
            var taskCategory = GetTaskCategoryFromOpId(operationOrder.OpId);

            var tasks = new List<ExecutionTask>();

            // 3. 根据频次类型和任务类别生成任务
            switch (operationOrder.FrequencyType)
            {
                case "每天":
                    GenerateDailyTasks(operationOrder, tasks, taskCategory);
                    break;
                case "持续":
                    GenerateContinuousTask(operationOrder, tasks, taskCategory);
                    break;
                case "一次性":
                    GenerateOneTimeTask(operationOrder, tasks, taskCategory);
                    break;
                default:
                    // 默认按一次性处理
                    GenerateOneTimeTask(operationOrder, tasks, taskCategory);
                    break;
            }

            return tasks;
        }

        /// <summary>
        /// 生成每天多次的任务（如：每天3次）
        /// </summary>
        private void GenerateDailyTasks(OperationOrder order, List<ExecutionTask> tasks, TaskCategory taskCategory)
        {
            // 解析频次值（如："3次" -> 3）
            if (!int.TryParse(order.FrequencyValue.Replace("次", "").Trim(), out int timesPerDay))
            {
                timesPerDay = 3; // 默认3次
            }

            // 计算每次执行的时间点（均匀分布在一天中）
            var startDate = order.CreateTime.Date;
            var endDate = order.PlantEndTime.Date;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                // 根据次数分配时间点（例如：3次 = 08:00, 14:00, 20:00）
                var timeSlots = CalculateTimeSlots(timesPerDay);
                
                foreach (var timeSlot in timeSlots)
                {
                    var plannedTime = currentDate.Add(timeSlot);
                    
                    // 确保不超过结束时间
                    if (plannedTime <= order.PlantEndTime)
                    {
                        // 根据 TaskCategory 决定是否需要执行时间
                        if (taskCategory == TaskCategory.Immediate)
                        {
                            // Immediate 任务：不需要 PlannedStartTime（立即执行，使用当前时间作为默认值）
                            tasks.Add(CreateOperationTask(order, null, taskCategory));
                        }
                        else
                        {
                            // Duration 和 ResultPending 任务：需要 PlannedStartTime（必须按时执行）
                            tasks.Add(CreateOperationTask(order, plannedTime, taskCategory));
                        }
                    }
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        /// <summary>
        /// 生成持续任务（如：持续24小时）
        /// 注意：FrequencyType="持续" 时，只有 Duration 类型的操作才适用
        /// </summary>
        private void GenerateContinuousTask(OperationOrder order, List<ExecutionTask> tasks, TaskCategory taskCategory)
        {
            // 持续任务只适用于 Duration 类型
            if (taskCategory != TaskCategory.Duration)
            {
                throw new ArgumentException($"操作 {order.OpId} 的类型为 {taskCategory}，不支持'持续'频次类型。只有 Duration 类型的操作才支持持续执行。");
            }

            // 解析持续时间（如："24小时" -> 24）
            if (!int.TryParse(order.FrequencyValue.Replace("小时", "").Trim(), out int hours))
            {
                hours = 24; // 默认24小时
            }

            // 持续任务：开始时间和结束时间
            var startTime = order.CreateTime;
            var endTime = startTime.AddHours(hours);
            
            // 确保不超过医嘱结束时间
            if (endTime > order.PlantEndTime)
            {
                endTime = order.PlantEndTime;
            }

            tasks.Add(new ExecutionTask
            {
                MedicalOrderId = order.Id,
                PatientId = order.PatientId,
                Category = TaskCategory.Duration, // 持续任务
                PlannedStartTime = startTime, // Duration 任务必须有执行时间
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                
                DataPayload = JsonSerializer.Serialize(new
                {
                    TaskType = "CONTINUOUS_OPERATION",
                    Title = GetOperationName(order.OpId),
                    Description = $"持续执行：{order.FrequencyValue}",
                    OpId = order.OpId,
                    Normal = order.Normal,
                    ExpectedDurationHours = hours,
                    PlannedEndTime = endTime, // 记录计划结束时间
                    RequiresScan = true // 需要扫码验证患者身份
                })
            });
        }

        /// <summary>
        /// 生成一次性任务
        /// </summary>
        private void GenerateOneTimeTask(OperationOrder order, List<ExecutionTask> tasks, TaskCategory taskCategory)
        {
            // 根据 TaskCategory 决定是否需要执行时间
            if (taskCategory == TaskCategory.Immediate)
            {
                // Immediate 任务：不需要 PlannedStartTime（立即执行）
                tasks.Add(CreateOperationTask(order, null, taskCategory));
            }
            else
            {
                // Duration 和 ResultPending 任务：使用医嘱创建时间作为执行时间
                tasks.Add(CreateOperationTask(order, order.CreateTime, taskCategory));
            }
        }

        /// <summary>
        /// 创建操作任务（通用方法）
        /// </summary>
        /// <param name="order">操作医嘱</param>
        /// <param name="plannedTime">计划执行时间。Immediate 任务为 null，Duration/ResultPending 任务必须有值</param>
        /// <param name="category">任务类别</param>
        private ExecutionTask CreateOperationTask(OperationOrder order, DateTime? plannedTime, TaskCategory category)
        {
            // 验证：Duration 和 ResultPending 任务必须有执行时间
            if (category != TaskCategory.Immediate && !plannedTime.HasValue)
            {
                throw new ArgumentException($"任务类别 {category} 必须指定执行时间（PlannedStartTime）");
            }

            // 确定 TaskType 字符串
            string taskType = category switch
            {
                TaskCategory.Duration => "DURATION_OPERATION",
                TaskCategory.ResultPending => "RESULT_PENDING_OPERATION",
                _ => "IMMEDIATE_OPERATION"
            };

            return new ExecutionTask
            {
                MedicalOrderId = order.Id,
                PatientId = order.PatientId,
                Category = category,
                PlannedStartTime = plannedTime ?? DateTime.UtcNow, // Immediate 任务使用当前时间作为默认值
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                
                DataPayload = JsonSerializer.Serialize(new
                {
                    TaskType = taskType,
                    Title = GetOperationName(order.OpId),
                    Description = $"操作代码：{order.OpId}，频次：{order.FrequencyType} {order.FrequencyValue}",
                    OpId = order.OpId,
                    Normal = order.Normal,
                    FrequencyType = order.FrequencyType,
                    FrequencyValue = order.FrequencyValue,
                    RequiresScan = true, // 需要扫码验证患者身份
                    IsImmediate = category == TaskCategory.Immediate, // 标记是否为立即执行
                    ExecutionMode = category == TaskCategory.Immediate ? "立即执行" 
                                   : category == TaskCategory.Duration ? "持续执行"
                                   : "等待结果" // ResultPending
                })
            };
        }

        /// <summary>
        /// 计算一天中的时间点分布
        /// </summary>
        private List<TimeSpan> CalculateTimeSlots(int timesPerDay)
        {
            // 默认时间分布策略
            var slots = new List<TimeSpan>();
            
            switch (timesPerDay)
            {
                case 1:
                    slots.Add(new TimeSpan(8, 0, 0)); // 08:00
                    break;
                case 2:
                    slots.Add(new TimeSpan(8, 0, 0));  // 08:00
                    slots.Add(new TimeSpan(20, 0, 0)); // 20:00
                    break;
                case 3:
                    slots.Add(new TimeSpan(8, 0, 0));  // 08:00
                    slots.Add(new TimeSpan(14, 0, 0)); // 14:00
                    slots.Add(new TimeSpan(20, 0, 0)); // 20:00
                    break;
                case 4:
                    slots.Add(new TimeSpan(8, 0, 0));  // 08:00
                    slots.Add(new TimeSpan(12, 0, 0)); // 12:00
                    slots.Add(new TimeSpan(16, 0, 0)); // 16:00
                    slots.Add(new TimeSpan(20, 0, 0)); // 20:00
                    break;
                default:
                    // 超过4次，均匀分布
                    var interval = 24.0 / timesPerDay;
                    for (int i = 0; i < timesPerDay; i++)
                    {
                        var hours = (int)(interval * i);
                        slots.Add(new TimeSpan(hours, 0, 0));
                    }
                    break;
            }
            
            return slots;
        }

        /// <summary>
        /// 根据操作代码获取操作名称
        /// </summary>
        private string GetOperationName(string opId)
        {
            return OperationNameMap.ContainsKey(opId) 
                ? OperationNameMap[opId] 
                : $"操作 {opId}";
        }
    }
}

