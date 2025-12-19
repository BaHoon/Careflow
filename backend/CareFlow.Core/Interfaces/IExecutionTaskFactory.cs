using System.Collections.Generic;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Core.Interfaces
{
    /// <summary>
    /// 领域服务接口：定义如何将医嘱转换为执行任务
    /// </summary>
    public interface IExecutionTaskFactory
    {
        /// <summary>
        /// 根据医嘱实体生成待执行任务列表（纯内存操作，不涉及数据库读写）
        /// </summary>
        /// <param name="medicalOrder">医嘱基类或具体子类</param>
        /// <returns>准备好保存的执行任务列表</returns>
        List<ExecutionTask> CreateTasks(CareFlow.Core.Models.Medical.MedicalOrder medicalOrder);
    }
}