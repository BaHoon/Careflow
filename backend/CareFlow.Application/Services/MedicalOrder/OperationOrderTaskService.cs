using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Application.Services
{
    public interface IOperationOrderTaskService
    {
        Task<List<ExecutionTask>> GenerateExecutionTasksAsync(OperationOrder order);
        Task RefreshExecutionTasksAsync(OperationOrder order);
        Task RollbackPendingTasksAsync(long orderId, string reason);
        Task CheckAndUpdateOrderStatusAsync(long orderId);
    }

    public class OperationOrderTaskService : IOperationOrderTaskService
    {
        private readonly IRepository<ExecutionTask, long> _taskRepository;
        private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
        private readonly IRepository<OperationOrder, long> _operationOrderRepository;
        private readonly IBarcodeService _barcodeService;
        private readonly ILogger<OperationOrderTaskService> _logger;
        private readonly OperationExecutionTaskFactory _taskFactory;

        public OperationOrderTaskService(
            IRepository<ExecutionTask, long> taskRepository,
            IRepository<BarcodeIndex, string> barcodeRepository,
            IRepository<OperationOrder, long> operationOrderRepository,
            IBarcodeService barcodeService,
            ILogger<OperationOrderTaskService> logger,
            OperationExecutionTaskFactory taskFactory)
        {
            _taskRepository = taskRepository;
            _barcodeRepository = barcodeRepository;
            _operationOrderRepository = operationOrderRepository;
            _barcodeService = barcodeService;
            _logger = logger;
            _taskFactory = taskFactory;
        }

        public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(OperationOrder order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            _logger.LogInformation("开始为操作医嘱 {OrderId} 生成执行任务", order.Id);

            // 1. 验证医嘱真实性
            var existingOrder = await _operationOrderRepository.GetQueryable()
                .FirstOrDefaultAsync(o => o.Id == order.Id);
            
            if (existingOrder == null)
            {
                throw new InvalidOperationException($"医嘱 {order.Id} 不存在，无法生成任务");
            }

            // 2. 状态检查
            if (existingOrder.Status == "Cancelled")
            {
                throw new InvalidOperationException($"医嘱 {order.Id} 已取消，操作终止");
            }

            // 3. 操作特有字段验证
            ValidateOperationFields(existingOrder);

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
                    _logger.LogWarning("工厂未生成任何任务，请检查医嘱的频次配置");
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

                        // 生成条形码索引（用于扫码验证）
                        await GenerateBarcodeForTask(task);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存任务失败，计划时间: {PlannedTime}", task.PlannedStartTime);
                        // 单个失败不中断整体流程
                    }
                }

                _logger.LogInformation("已为医嘱 {OrderId} 成功生成 {Count} 个任务", order.Id, savedTasks.Count);
                
                // 6. 检查是否需要更新医嘱状态
                await CheckAndUpdateOrderStatusAsync(existingOrder.Id);
                
                return savedTasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成执行任务流程发生错误");
                throw;
            }
        }

        /// <summary>
        /// 检查所有任务是否完成，如果完成则更新医嘱的EndTime
        /// </summary>
        public async Task CheckAndUpdateOrderStatusAsync(long orderId)
        {
            try
            {
                var order = await _operationOrderRepository.GetByIdAsync(orderId);
                if (order == null) return;

                // 查询该医嘱的所有任务
                var allTasks = await _taskRepository.ListAsync(t => t.MedicalOrderId == orderId);
                
                if (!allTasks.Any())
                {
                    _logger.LogWarning("医嘱 {OrderId} 没有关联的任务", orderId);
                    return;
                }

                // 检查是否所有任务都已完成（Completed或Cancelled）
                var incompleteTasks = allTasks.Where(t => 
                    t.Status != "Completed" && 
                    t.Status != "Cancelled" && 
                    t.Status != "Skipped").ToList();

                if (!incompleteTasks.Any() && !order.EndTime.HasValue)
                {
                    // 所有任务都已完成，更新EndTime
                    order.EndTime = DateTime.UtcNow;
                    order.Status = "Completed";
                    await _operationOrderRepository.UpdateAsync(order);
                    _logger.LogInformation("医嘱 {OrderId} 的所有任务已完成，已更新EndTime", orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查并更新医嘱状态失败，OrderId: {OrderId}", orderId);
                // 不抛出异常，避免影响主流程
            }
        }

        public async Task RefreshExecutionTasksAsync(OperationOrder order)
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
                t.Status == "Pending");

            foreach (var task in pendingTasks)
            {
                task.Status = "Cancelled";
                task.IsRolledBack = true;
                task.ExceptionReason = reason;
                task.LastModifiedAt = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(task);
            }
        }

        #region 私有辅助方法

        private void ValidateOperationFields(OperationOrder order)
        {
            if (string.IsNullOrWhiteSpace(order.OpId))
                throw new ArgumentException("操作代码不能为空");

            if (string.IsNullOrWhiteSpace(order.FrequencyType))
                throw new ArgumentException("频次类型不能为空");

            if (string.IsNullOrWhiteSpace(order.FrequencyValue))
                throw new ArgumentException("频次值不能为空");

            // 验证结束时间
            if (order.PlantEndTime <= order.CreateTime)
            {
                throw new ArgumentException("医嘱结束时间必须晚于创建时间");
            }

            // 验证IsLongTerm和FrequencyType的匹配关系
            if (!order.IsLongTerm)
            {
                // 临时医嘱：FrequencyType必须为"一次性"
                if (order.FrequencyType != "一次性")
                {
                    throw new ArgumentException($"临时医嘱（IsLongTerm=false）的FrequencyType必须为'一次性'，当前值：{order.FrequencyType}");
                }
            }
            else
            {
                // 长期医嘱：FrequencyType必须为"x天y次"格式
                if (!order.FrequencyType.Contains("天") || !order.FrequencyType.Contains("次"))
                {
                    throw new ArgumentException($"长期医嘱（IsLongTerm=true）的FrequencyType必须为'x天y次'格式，当前值：{order.FrequencyType}");
                }
            }
        }

        private async Task<bool> HasPendingTasksAsync(long orderId)
        {
            var tasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == "Pending" || t.Status == "Running"));
            return tasks.Any();
        }

        /// <summary>
        /// 为任务生成条形码索引（用于扫码验证患者身份）
        /// </summary>
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
                
                _logger.LogDebug("为任务 {TaskId} 生成条形码索引成功", task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为任务 {TaskId} 生成条形码失败", task.Id);
                // 条形码生成失败不影响任务创建，只记录日志
            }
        }

        #endregion
    }
}

