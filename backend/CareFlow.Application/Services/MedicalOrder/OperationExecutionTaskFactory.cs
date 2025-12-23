using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Enums;
using CareFlow.Core.Utils;

namespace CareFlow.Application.Services
{
    /// <summary>
    /// 操作类医嘱任务生成工厂
    /// 根据 IsLongTerm、FrequencyType 和 FrequencyValue 生成执行任务
    /// TaskCategory 仅根据操作代码（OpId）确定，与 FrequencyType、FrequencyValue 无关
    /// </summary>
    public class OperationExecutionTaskFactory : IExecutionTaskFactory
    {
        // 操作代码到操作名称的映射（后续可以改为从数据库读取）
        private static readonly Dictionary<string, string> OperationNameMap = new()
        {
            { "OP001", "更换引流袋" },
            { "OP002", "持续吸氧" },
            { "OP003", "血糖监测" },
            { "OP004", "更换敷料" },
            { "OP005", "导尿" },
            { "OP006", "鼻饲" },
            { "OP007", "雾化吸入" },
            { "OP008", "口腔护理" },
            { "OP009", "会阴护理" },
            { "OP010", "皮肤护理" },
            { "OP011", "持续心电监护" },
            { "OP012", "持续导尿" },
            { "OP013", "持续胃肠减压" },
            { "OP014", "持续静脉输液" },
            { "OP015", "翻身拍背" },
            { "OP016", "血压监测" },
            { "OP017", "体温监测" },
            { "OP018", "尿量监测" },
            { "OP019", "意识状态评估" },
            { "OP020", "疼痛评估" }
        };

        /// <summary>
        /// 根据操作代码（OpId）获取任务类别
        /// </summary>
        private TaskCategory GetTaskCategoryFromOpId(string opId)
        {
            return opId switch
            {
                // Immediate 类：即刻执行，扫码即完成
                "OP001" => TaskCategory.Immediate,  // 更换引流袋
                "OP004" => TaskCategory.Immediate,  // 更换敷料
                "OP005" => TaskCategory.Immediate,  // 导尿
                "OP008" => TaskCategory.Immediate,  // 口腔护理
                "OP009" => TaskCategory.Immediate,  // 会阴护理
                "OP010" => TaskCategory.Immediate,  // 皮肤护理
                "OP015" => TaskCategory.Immediate,  // 翻身拍背
                
                // Duration 类：持续执行，需要开始和结束时间
                "OP002" => TaskCategory.Duration,   // 持续吸氧
                "OP006" => TaskCategory.Duration,   // 鼻饲
                "OP007" => TaskCategory.Duration,   // 雾化吸入
                "OP011" => TaskCategory.Duration,   // 持续心电监护
                "OP012" => TaskCategory.Duration,   // 持续导尿
                "OP013" => TaskCategory.Duration,   // 持续胃肠减压
                "OP014" => TaskCategory.Duration,   // 持续静脉输液
                
                // ResultPending 类：需要等待结果，录入结果后完成
                "OP003" => TaskCategory.ResultPending, // 血糖监测
                "OP016" => TaskCategory.ResultPending, // 血压监测
                "OP017" => TaskCategory.ResultPending, // 体温监测
                "OP018" => TaskCategory.ResultPending, // 尿量监测
                "OP019" => TaskCategory.ResultPending, // 意识状态评估
                "OP020" => TaskCategory.ResultPending, // 疼痛评估
                
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

            // 3. 根据 IsLongTerm 决定任务生成策略
            if (operationOrder.IsLongTerm)
            {
                // 长期医嘱：根据"x天y次"生成多个任务
                GenerateLongTermTasks(operationOrder, tasks, taskCategory);
            }
            else
            {
                // 临时医嘱：只生成1个任务
                GenerateOneTimeTask(operationOrder, tasks, taskCategory);
            }

            // 4. 验证和过滤时间：如果只生成一个任务且时间已过则报错，如果多个任务则删除时间已过的任务
            ValidateAndFilterTaskTime(tasks);

            return tasks;
        }

        /// <summary>
        /// 生成长期医嘱任务（IsLongTerm=true）
        /// FrequencyType格式："x天y次"（如"1天3次"）
        /// - 如果x=1，表示每天执行y次
        /// - 如果x=2，表示每2天执行y次（第1天、第3天、第5天...各执行y次）
        /// FrequencyValue格式：时间点列表，用逗号或分号分隔（如"08:00,12:00,16:00"），应包含y个时间点
        /// 
        /// 计算逻辑：
        /// 1. 计算从当前日期（CreateTime.Date）到PlantEndTime.Date的总天数
        /// 2. 计算包含多少个x天周期：从第1天开始，每x天为一个周期，直到超过结束日期
        /// 3. 任务数量 = 周期数 × y
        /// 4. 每个任务的PlannedStartTime = 对应日期 + FrequencyValue中的时间点
        /// 
        /// 例如：1天3次，时间点08:00,12:00,16:00，医嘱进行3天（从2024-01-01到2024-01-03）
        /// - 总天数：3天
        /// - 周期数：3个周期（第1天、第2天、第3天各为一个周期，因为x=1）
        /// - 任务数量：3 × 3 = 9个任务
        /// - 任务时间：
        ///   第1天：2024-01-01 08:00, 2024-01-01 12:00, 2024-01-01 16:00
        ///   第2天：2024-01-02 08:00, 2024-01-02 12:00, 2024-01-02 16:00
        ///   第3天：2024-01-03 08:00, 2024-01-03 12:00, 2024-01-03 16:00
        /// </summary>
        private void GenerateLongTermTasks(OperationOrder order, List<ExecutionTask> tasks, TaskCategory taskCategory)
        {
            // 解析FrequencyType：提取x（天数）和y（次数）
            var frequencyParts = order.FrequencyType.Split('天');
            if (frequencyParts.Length != 2)
            {
                throw new ArgumentException($"长期医嘱的FrequencyType格式错误，应为'x天y次'，当前值：{order.FrequencyType}");
            }

            if (!int.TryParse(frequencyParts[0].Trim(), out int daysPerCycle))
            {
                throw new ArgumentException($"无法解析天数，FrequencyType：{order.FrequencyType}");
            }

            var timesPart = frequencyParts[1].Replace("次", "").Trim();
            if (!int.TryParse(timesPart, out int timesPerCycle))
            {
                throw new ArgumentException($"无法解析次数，FrequencyType：{order.FrequencyType}");
            }

            // 解析FrequencyValue：时间点列表（应包含y个时间点）
            var timePoints = ParseTimePoints(order.FrequencyValue);
            if (timePoints.Count != timesPerCycle)
            {
                throw new ArgumentException($"时间点数量({timePoints.Count})与次数({timesPerCycle})不匹配");
            }

            // 计算从当前日期（CreateTime.Date）到PlantEndTime.Date的总天数
            // 注意：CreateTime 和 PlantEndTime 在数据库中存储为中国时间，直接使用
            var startDate = order.CreateTime.Date;
            var endDate = order.PlantEndTime.Date;

            // 按周期生成任务：从startDate开始，每x天为一个周期
            // 例如：x=1，从第1天开始，每天执行y次
            // 例如：x=2，从第1天开始，每2天执行y次（第1天、第3天、第5天...）
            var currentDate = startDate;
            int cycleIndex = 0;

            while (currentDate <= endDate)
            {
                // 为当前周期的这一天生成y个任务（对应y个时间点）
                foreach (var timePoint in timePoints)
                {
                    // 任务的PlannedStartTime = 当前日期 + FrequencyValue中的时间点（中国时间）
                    var plannedTimeChina = currentDate.Add(timePoint);
                    
                    // 确保不超过结束时间（直接比较，都是中国时间）
                    if (plannedTimeChina <= order.PlantEndTime)
                    {
                        // 直接存储中国时间到数据库
                        var plannedTime = TimeZoneHelper.StoreChinaTime(plannedTimeChina);
                        tasks.Add(CreateOperationTask(order, plannedTime, taskCategory));
                    }
                }

                // 移动到下一个周期：当前日期 + x天
                cycleIndex++;
                currentDate = startDate.AddDays(cycleIndex * daysPerCycle);
            }
        }

        /// <summary>
        /// 生成一次性任务（IsLongTerm=false）
        /// FrequencyType必须为"一次性"
        /// FrequencyValue为"立即"或固定时间点（如"14:30"）
        /// 
        /// PlannedStartTime计算逻辑：
        /// - 如果FrequencyValue="立即"：PlannedStartTime = 当前日期当前时间点（DateTime.UtcNow）
        /// - 如果FrequencyValue=固定时间点（如"14:30"）：PlannedStartTime = 当前日期 + 固定时间点
        /// </summary>
        private void GenerateOneTimeTask(OperationOrder order, List<ExecutionTask> tasks, TaskCategory taskCategory)
        {
            // 验证FrequencyType
            if (order.FrequencyType != "一次性")
            {
                throw new ArgumentException($"临时医嘱的FrequencyType必须为'一次性'，当前值：{order.FrequencyType}");
            }

            DateTime plannedTime;

            // 解析FrequencyValue（使用中国时间）
            var chinaTimeNow = TimeZoneHelper.GetChinaTimeNow();
            
            if (order.FrequencyValue == "立即" || string.IsNullOrWhiteSpace(order.FrequencyValue))
            {
                // 立即执行：PlannedStartTime = 当前中国时间
                plannedTime = TimeZoneHelper.StoreChinaTime(chinaTimeNow);
            }
            else
            {
                // 固定时间点：解析时间字符串（如"14:30"）
                if (TimeSpan.TryParse(order.FrequencyValue, out var timeSpan))
                {
                    // PlannedStartTime = 当前中国日期 + 固定时间点
                    var plannedTimeChina = chinaTimeNow.Date.Add(timeSpan);
                    plannedTime = TimeZoneHelper.StoreChinaTime(plannedTimeChina);
                }
                else
                {
                    // 尝试解析完整日期时间（假设输入的是中国时间）
                    if (DateTime.TryParse(order.FrequencyValue, out var dateTime))
                    {
                        // 输入的时间假设为中国时间，直接存储
                        plannedTime = TimeZoneHelper.StoreChinaTime(dateTime);
                    }
                    else
                    {
                        throw new ArgumentException($"无法解析FrequencyValue时间格式：{order.FrequencyValue}");
                    }
                }
            }

            // 确保计划时间不超过结束时间（直接比较，都是中国时间）
            var plannedTimeChinaCheck = TimeZoneHelper.ToChinaTime(plannedTime);
            var endTimeChina = TimeZoneHelper.ToChinaTime(order.PlantEndTime);
            if (plannedTimeChinaCheck > endTimeChina)
            {
                throw new ArgumentException($"计划执行时间({plannedTimeChinaCheck:yyyy-MM-dd HH:mm:ss} 北京时间)不能晚于医嘱结束时间({endTimeChina:yyyy-MM-dd HH:mm:ss} 北京时间)");
            }

            tasks.Add(CreateOperationTask(order, plannedTime, taskCategory));
        }

        /// <summary>
        /// 解析时间点字符串（如"08:00,14:00,20:00"或"08:00;14:00;20:00"）
        /// </summary>
        private List<TimeSpan> ParseTimePoints(string frequencyValue)
        {
            var timePoints = new List<TimeSpan>();
            
            // 支持逗号或分号分隔
            var separators = new[] { ',', ';', '，', '；' };
            var parts = frequencyValue.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (TimeSpan.TryParse(trimmed, out var timeSpan))
                {
                    timePoints.Add(timeSpan);
                }
                else
                {
                    throw new ArgumentException($"无法解析时间点：{trimmed}");
                }
            }

            // 按时间顺序排序
            timePoints.Sort();
            return timePoints;
        }

        /// <summary>
        /// 创建操作任务（通用方法）
        /// </summary>
        /// <param name="order">操作医嘱</param>
        /// <param name="plannedTime">计划执行时间。所有任务都必须有明确的执行时间</param>
        /// <param name="category">任务类别</param>
        private ExecutionTask CreateOperationTask(OperationOrder order, DateTime plannedTime, TaskCategory category)
        {

            // 确定 TaskType 字符串
            string taskType = category switch
            {
                TaskCategory.Duration => "DURATION_OPERATION",
                TaskCategory.ResultPending => "RESULT_PENDING_OPERATION",
                _ => "IMMEDIATE_OPERATION"
            };

            // 为Category=3（ResultPending）的任务生成结构化数据模板（放在DataPayload中供前端使用）
            // 但ResultPayload初始为null，等护士输入后才算任务结束
            string? resultPayloadTemplate = null;
            if (category == TaskCategory.ResultPending)
            {
                resultPayloadTemplate = GenerateResultPayloadTemplate(order.OpId);
            }

            var dataPayload = new
            {
                TaskType = taskType,
                Title = GetOperationName(order.OpId),
                Description = $"操作代码：{order.OpId}，频次：{order.FrequencyType} {order.FrequencyValue}",
                OpId = order.OpId,
                Normal = order.Normal,
                FrequencyType = order.FrequencyType,
                FrequencyValue = order.FrequencyValue,
                RequiresScan = true,
                IsImmediate = category == TaskCategory.Immediate,
                ExecutionMode = category == TaskCategory.Immediate ? "立即执行" 
                               : category == TaskCategory.Duration ? "持续执行"
                               : "等待结果",
                ResultPayloadTemplate = resultPayloadTemplate // Category=3的任务包含结果模板（供前端使用）
            };

            return new ExecutionTask
            {
                MedicalOrderId = order.Id,
                PatientId = order.PatientId,
                Category = category,
                PlannedStartTime = plannedTime, // 所有任务都有明确的计划开始时间（中国时间存储）
                Status = "Pending",
                CreatedAt = TimeZoneHelper.StoreChinaTime(TimeZoneHelper.GetChinaTimeNow()), // 使用中国时间，直接存储
                ResultPayload = null, // 初始为null，Category=3的任务在护士输入结果后才算任务结束
                DataPayload = JsonSerializer.Serialize(dataPayload, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
            };
        }

        /// <summary>
        /// 为Category=3（ResultPending）的操作生成结构化结果数据模板
        /// 护士只需要填写核心结果（如数字或其他）
        /// </summary>
        private string? GenerateResultPayloadTemplate(string opId)
        {
            return opId switch
            {
                "OP003" => JsonSerializer.Serialize(new { Value = 0.0, Unit = "mmol/L", Note = "" }), // 血糖监测：数值+单位+备注
                "OP016" => JsonSerializer.Serialize(new { Systolic = 0, Diastolic = 0, Note = "" }), // 血压监测：收缩压+舒张压+备注
                "OP017" => JsonSerializer.Serialize(new { Value = 0.0, Unit = "℃", Note = "" }), // 体温监测：数值+单位+备注
                "OP018" => JsonSerializer.Serialize(new { Value = 0.0, Unit = "ml", Note = "" }), // 尿量监测：数值+单位+备注
                "OP019" => JsonSerializer.Serialize(new { Level = "", Description = "", Note = "" }), // 意识状态评估：等级+描述+备注
                "OP020" => JsonSerializer.Serialize(new { Score = 0, Level = "", Note = "" }), // 疼痛评估：评分+等级+备注
                _ => null // 其他操作不需要结果模板
            };
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

        /// <summary>
        /// 验证和过滤任务时间：
        /// 1. 如果只生成一个任务，检查PlannedStartTime是否已过，如果已过则报错且不产生任务
        /// 2. 如果生成多个任务（长期医嘱），自动删除计划开始时间早于当前时间的任务
        /// 
        /// 重要说明（长期医嘱场景）：
        /// - 对于长期医嘱（如1天3次，持续3天），会先生成所有日期的所有时间点任务
        /// - 然后过滤掉所有时间已过的任务（包括今天已过的时间点）
        /// - 后续天数的所有时间点任务都会正常生成（不会被过滤）
        /// 
        /// 示例：当前时间2024-01-01 14:00，医嘱1天3次（08:00,12:00,16:00），持续3天
        /// - 生成：今天08:00,12:00,16:00，明天08:00,12:00,16:00，后天08:00,12:00,16:00
        /// - 过滤：删除今天08:00和12:00（已过），保留今天16:00
        /// - 保留：明天和后天的所有时间点任务（08:00,12:00,16:00）都正常生成
        /// </summary>
        private void ValidateAndFilterTaskTime(List<ExecutionTask> tasks)
        {
            // 使用当前中国时间进行比较
            // 数据库中的时间也是中国时间，直接比较
            var currentTimeChina = TimeZoneHelper.GetChinaTimeNow();
            
            // 如果只生成一个任务，需要检查时间是否已过
            if (tasks.Count == 1)
            {
                var task = tasks[0];
                // 数据库存储的是中国时间，直接比较
                var taskTimeChina = TimeZoneHelper.ToChinaTime(task.PlannedStartTime);
                
                // 如果任务的计划开始时间已经过去，报错且不产生任务
                if (taskTimeChina < currentTimeChina)
                {
                    tasks.Clear(); // 清空任务列表，不产生任务
                    throw new ArgumentException(
                        $"不能选择已经过去的时间。任务的计划开始时间({taskTimeChina:yyyy-MM-dd HH:mm:ss} 北京时间) " +
                        $"早于当前时间({currentTimeChina:yyyy-MM-dd HH:mm:ss} 北京时间)。请选择未来的时间。");
                }
            }
            // 如果生成多个任务（长期医嘱），自动删除计划开始时间早于当前时间的任务
            // 注意：这里会删除所有时间已过的任务，包括今天已过的时间点
            // 但后续天数的所有时间点任务都会正常保留（因为它们的PlannedStartTime >= currentTimeChina）
            else if (tasks.Count > 1)
            {
                var tasksToRemove = tasks.Where(t => 
                {
                    var taskTime = TimeZoneHelper.ToChinaTime(t.PlannedStartTime);
                    return taskTime < currentTimeChina;
                }).ToList();
                
                foreach (var task in tasksToRemove)
                {
                    tasks.Remove(task);
                }
                
                // 如果删除后没有剩余任务，报错
                if (!tasks.Any())
                {
                    throw new ArgumentException(
                        $"所有任务的计划开始时间都早于当前时间({currentTimeChina:yyyy-MM-dd HH:mm:ss} 北京时间)。请调整医嘱的时间范围。");
                }
            }
        }
    }
}
