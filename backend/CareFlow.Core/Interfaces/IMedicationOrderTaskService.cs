using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Core.Interfaces;

public interface IMedicationOrderTaskService
{
    /// <summary>
    /// 根据medication order生成执行任务
    /// </summary>
    /// <param name="order">医嘱对象</param>
    /// <returns>生成的执行任务列表</returns>
    Task<List<ExecutionTask>> GenerateExecutionTasksAsync(MedicationOrder order);
    
    /// <summary>
    /// 医嘱停止时回滚未执行的任务
    /// </summary>
    /// <param name="orderId">医嘱ID</param>
    /// <param name="reason">回滚原因</param>
    Task RollbackPendingTasksAsync(long orderId, string reason);
    
    /// <summary>
    /// 重新计算并更新执行任务（医嘱修改时）
    /// </summary>
    /// <param name="order">修改后的医嘱对象</param>
    Task RefreshExecutionTasksAsync(MedicationOrder order);
}