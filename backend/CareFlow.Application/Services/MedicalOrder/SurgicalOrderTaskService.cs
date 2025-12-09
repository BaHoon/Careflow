using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Application.Services
{
    public interface ISurgicalOrderTaskService
    {
        Task<List<ExecutionTask>> GenerateExecutionTasksAsync(SurgicalOrder order);
        Task RefreshExecutionTasksAsync(SurgicalOrder order);
        Task RollbackPendingTasksAsync(long orderId, string reason);
    }

    public class SurgicalOrderTaskService : ISurgicalOrderTaskService
    {
        private readonly IRepository<ExecutionTask, long> _taskRepository;
        private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
        private readonly IRepository<SurgicalOrder, long> _surgicalOrderRepository;
        private readonly IBarcodeService _barcodeService;
        private readonly ILogger<SurgicalOrderTaskService> _logger;
        private readonly IExecutionTaskFactory _taskFactory; // 注入工厂

        public SurgicalOrderTaskService(
            IRepository<ExecutionTask, long> taskRepository,
            IRepository<BarcodeIndex, string> barcodeRepository,
            IRepository<SurgicalOrder, long> surgicalOrderRepository,
            IBarcodeService barcodeService,
            ILogger<SurgicalOrderTaskService> logger,
            IExecutionTaskFactory taskFactory)
        {
            _taskRepository = taskRepository;
            _barcodeRepository = barcodeRepository;
            _surgicalOrderRepository = surgicalOrderRepository;
            _barcodeService = barcodeService;
            _logger = logger;
            _taskFactory = taskFactory;
        }

        public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(SurgicalOrder order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            _logger.LogInformation("开始为手术医嘱 {OrderId} 生成执行任务", order.Id);

            // 1. 验证医嘱真实性
            var existingOrder = await _surgicalOrderRepository.GetByIdAsync(order.Id);
            if (existingOrder == null)
            {
                throw new InvalidOperationException($"医嘱 {order.Id} 不存在，无法生成任务");
            }

            // 2. 状态检查 (防止对已取消的医嘱生成任务)
            if (existingOrder.Status == "Cancelled")
            {
                throw new InvalidOperationException($"医嘱 {order.Id} 已取消，操作终止");
            }

            // 3. 手术特有字段验证
            ValidateSurgicalFields(existingOrder);

            // 4. 检查重复任务
            if (await HasPendingTasksAsync(existingOrder.Id))
            {
                _logger.LogWarning("医嘱 {OrderId} 已存在未完成的任务", order.Id);
                // 策略：记录日志但不抛出异常，允许追加任务
            }

            var savedTasks = new List<ExecutionTask>();

            try
            {
                // === 核心：调用工厂生成内存中的任务列表 ===
                var tasksToCreate = _taskFactory.CreateTasks(existingOrder);

                if (!tasksToCreate.Any())
                {
                    _logger.LogWarning("工厂未生成任何任务，请检查医嘱的JSON配置字段 (Talk/Operation/Meds)");
                    return savedTasks;
                }

                // 5. 批量保存并生成条形码
                foreach (var task in tasksToCreate)
                {
                    try
                    {
                        // 保存任务获取ID
                        await _taskRepository.AddAsync(task);
                        savedTasks.Add(task);

                        // 生成条形码
                        await GenerateBarcodeForTask(task);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存任务失败，计划时间: {PlannedTime}", task.PlannedStartTime);
                        // 单个失败不中断整体流程
                    }
                }

                _logger.LogInformation("已为医嘱 {OrderId} 成功生成 {Count} 个任务", order.Id, savedTasks.Count);
                return savedTasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成执行任务流程发生错误");
                throw;
            }
        }

        public async Task RefreshExecutionTasksAsync(SurgicalOrder order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            
            _logger.LogInformation("正在刷新医嘱 {OrderId} 的任务...", order.Id);
            
            // 1. 回滚旧任务
            await RollbackPendingTasksAsync(order.Id, "医嘱变更重置");
            
            // 2. 生成新任务
            await GenerateExecutionTasksAsync(order);
        }

        public async Task RollbackPendingTasksAsync(long orderId, string reason)
        {
            if (orderId <= 0) throw new ArgumentException("ID无效");

            _logger.LogInformation("回滚医嘱 {OrderId} 的未完成任务，原因: {Reason}", orderId, reason);

            var pendingTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                t.Status == "Pending"); // 假设 Pending 是字符串

            foreach (var task in pendingTasks)
            {
                task.Status = "Cancelled";
                task.ExceptionReason = reason;
                task.LastModifiedAt = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(task);
            }
        }

        #region 私有辅助方法

        private void ValidateSurgicalFields(SurgicalOrder order)
        {
            if (string.IsNullOrWhiteSpace(order.SurgeryName))
                throw new ArgumentException("手术名称不能为空");

            if (order.ScheduleTime == default)
                throw new ArgumentException("手术排期时间无效");

            // 允许一定的时间误差，但不能太离谱
            if (order.ScheduleTime < DateTime.UtcNow.AddDays(-30))
            {
                _logger.LogWarning("手术排期时间 {Time} 过于久远", order.ScheduleTime);
            }
        }

        private async Task<bool> HasPendingTasksAsync(long orderId)
        {
            var tasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == "Pending" || t.Status == "InProgress"));
            return tasks.Any();
        }

        private async Task GenerateBarcodeForTask(ExecutionTask task)
        {
            try
            {
                var barcodeIndex = new BarcodeIndex
                {
                    Id = $"Exec-{task.Id}", 
                    TableName = "ExecutionTasks",
                    RecordId = task.Id.ToString()
                };
                await _barcodeRepository.AddAsync(barcodeIndex);
                // 如果需要生成物理图片: _barcodeService.Generate(barcodeIndex)...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为任务 {TaskId} 生成条形码失败", task.Id);
            }
        }

        #endregion
    }
}