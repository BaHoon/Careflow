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
using CareFlow.Core.Utils;

namespace CareFlow.Application.Services
{
    public interface IOperationOrderTaskService
    {
        Task<List<ExecutionTask>> GenerateExecutionTasksAsync(long orderId);
        Task RefreshExecutionTasksAsync(long orderId);
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

        /// <summary>
        /// 根据医嘱ID生成执行任务
        /// 流程：输入医嘱ID -> 查询医嘱表 -> 根据逻辑拆分任务 -> 保存到任务表
        /// </summary>
        public async Task<List<ExecutionTask>> GenerateExecutionTasksAsync(long orderId)
        {
            if (orderId <= 0) throw new ArgumentException("医嘱ID无效", nameof(orderId));

            _logger.LogInformation("开始为操作医嘱 {OrderId} 生成执行任务", orderId);

            // 1. 根据ID查询医嘱表
            var order = await _operationOrderRepository.GetQueryable()
                .FirstOrDefaultAsync(o => o.Id == orderId);
            
            if (order == null)
            {
                throw new InvalidOperationException($"医嘱 {orderId} 不存在，无法生成任务");
            }

            // 2. 状态检查
            if (order.Status == "Cancelled")
            {
                throw new InvalidOperationException($"医嘱 {orderId} 已取消，操作终止");
            }

            // 3. 操作特有字段验证
            ValidateOperationFields(order);

            // 4. 检查重复任务
            if (await HasPendingTasksAsync(orderId))
            {
                _logger.LogWarning("医嘱 {OrderId} 已存在未完成的任务", orderId);
                // 策略：记录日志但不抛出异常，允许追加任务
            }

            var savedTasks = new List<ExecutionTask>();

            try
            {
                // === 核心：调用工厂根据医嘱逻辑拆分任务 ===
                var tasksToCreate = _taskFactory.CreateTasks(order);

                if (!tasksToCreate.Any())
                {
                    _logger.LogWarning("工厂未生成任何任务，请检查医嘱的频次配置");
                    return savedTasks;
                }

                // 5. 批量保存任务到任务表并生成条形码
                foreach (var task in tasksToCreate)
                {
                    try
                    {
                        // 保存任务到 ExecutionTasks 表
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

                _logger.LogInformation("已为医嘱 {OrderId} 成功生成 {Count} 个任务", orderId, savedTasks.Count);
                
                // 6. 检查是否需要更新医嘱状态
                await CheckAndUpdateOrderStatusAsync(orderId);
                
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
                    // 所有任务都已完成，更新EndTime（使用中国时间，直接存储）
                    order.EndTime = TimeZoneHelper.StoreChinaTime(TimeZoneHelper.GetChinaTimeNow());
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

        public async Task RefreshExecutionTasksAsync(long orderId)
        {
            if (orderId <= 0) throw new ArgumentException("医嘱ID无效", nameof(orderId));
            
            _logger.LogInformation("正在刷新医嘱 {OrderId} 的任务...", orderId);
            
            // 1. 回滚旧任务
            await RollbackPendingTasksAsync(orderId, "医嘱变更重置");
            
            // 2. 生成新任务
            await GenerateExecutionTasksAsync(orderId);
        }

        public async Task RollbackPendingTasksAsync(long orderId, string reason)
        {
            if (orderId <= 0) throw new ArgumentException("ID无效");

            _logger.LogInformation("回滚医嘱 {OrderId} 的未完成任务，原因: {Reason}", orderId, reason);

            // 查询所有未完成的任务（Pending和Running状态）
            var incompleteTasks = await _taskRepository.ListAsync(t => 
                t.MedicalOrderId == orderId && 
                (t.Status == "Pending" || t.Status == "Running"));

            if (!incompleteTasks.Any())
            {
                _logger.LogInformation("医嘱 {OrderId} 没有需要回滚的任务", orderId);
                return;
            }

            int deletedCount = 0;
            foreach (var task in incompleteTasks)
            {
                try
                {
                    // 1. 删除任务相关的条形码索引
                    var barcodeId = $"Exec-{task.Id}";
                    var barcode = await _barcodeRepository.GetByIdAsync(barcodeId);
                    if (barcode != null)
                    {
                        await _barcodeRepository.DeleteAsync(barcode);
                        _logger.LogDebug("已删除任务 {TaskId} 的条形码索引", task.Id);
                    }

                    // 2. 删除任务（物理删除）
                    await _taskRepository.DeleteAsync(task);
                    deletedCount++;

                    _logger.LogDebug("已删除任务 {TaskId}，计划时间: {PlannedTime}", 
                        task.Id, task.PlannedStartTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除任务 {TaskId} 失败", task.Id);
                    // 单个任务删除失败不影响其他任务
                }
            }

            _logger.LogInformation("医嘱 {OrderId} 回滚完成，共删除 {Count} 个任务", orderId, deletedCount);

            // 3. 删除医嘱（物理删除）
            try
            {
                var order = await _operationOrderRepository.GetByIdAsync(orderId);
                if (order != null)
                {
                    await _operationOrderRepository.DeleteAsync(order);
                    _logger.LogInformation("已删除医嘱 {OrderId}", orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医嘱 {OrderId} 失败", orderId);
                throw; // 医嘱删除失败应该抛出异常
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

