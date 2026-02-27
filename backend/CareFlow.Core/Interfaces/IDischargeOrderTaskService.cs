using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Core.Interfaces;

/// <summary>
/// 出院医嘱任务拆分服务接口
/// </summary>
public interface IDischargeOrderTaskService
{
    /// <summary>
    /// 生成出院医嘱的执行任务
    /// 任务一：取药任务（如果有出院带药，Category=Verification，提前2小时）
    /// 任务二：出院确认任务（Category=DischargeConfirmation，按出院时间）
    /// </summary>
    /// <param name="order">出院医嘱实体</param>
    /// <returns>生成的执行任务列表</returns>
    Task<List<ExecutionTask>> GenerateExecutionTasksAsync(DischargeOrder order);
}
