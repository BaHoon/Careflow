using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services
{
    /// <summary>
    /// 操作医嘱管理服务（独立入口，不修改 MedicalOrderManager）
    /// </summary>
    public interface IOperationOrderManager
    {
        Task<OperationOrder> CreateOperationOrderAsync(OperationOrder order);
    }

    public class OperationOrderManager : IOperationOrderManager
    {
        private readonly INurseAssignmentService _nurseAssignmentService;
        private readonly IRepository<OperationOrder, long> _operationRepo;
        private readonly IOperationOrderTaskService _taskService;
        private readonly ILogger<OperationOrderManager> _logger;

        public OperationOrderManager(
            INurseAssignmentService nurseAssignmentService,
            IRepository<OperationOrder, long> operationRepo,
            IOperationOrderTaskService taskService,
            ILogger<OperationOrderManager> logger)
        {
            _nurseAssignmentService = nurseAssignmentService;
            _operationRepo = operationRepo;
            _taskService = taskService;
            _logger = logger;
        }

        public async Task<OperationOrder> CreateOperationOrderAsync(OperationOrder order)
        {
            // 1. 设置默认值
            if (string.IsNullOrEmpty(order.Status))
            {
                order.Status = "Pending"; // 初始状态
            }

            if (order.CreateTime == default)
            {
                order.CreateTime = DateTime.UtcNow;
            }

            // 2. 先保存到数据库（确保实体有ID，用于后续更新）
            // 注意：由于使用 TPT 继承，EF Core 会同时保存到 MedicalOrders 基表和 OperationOrders 子表
            await _operationRepo.AddAsync(order);

            _logger.LogInformation("操作医嘱已保存到数据库: OrderId={OrderId}, OpId={OpId}, PatientId={PatientId}", 
                order.Id, order.OpId, order.PatientId);

            // 3. 自动分配护士（如果未指定）- 在保存后分配，然后更新
            if (string.IsNullOrEmpty(order.NurseId))
            {
                try
                {
                    // 操作医嘱使用当前时间查询排班
                    var targetTime = DateTime.UtcNow;
                    
                    var responsibleNurseId = await _nurseAssignmentService
                        .CalculateResponsibleNurseAsync(order.PatientId, targetTime);
                    
                    if (!string.IsNullOrEmpty(responsibleNurseId))
                    {
                        order.NurseId = responsibleNurseId;
                        // 更新数据库，确保 NurseId 被保存到基表 MedicalOrders
                        await _operationRepo.UpdateAsync(order);
                        _logger.LogInformation("✅ 已分配负责护士: {NurseId} 给操作医嘱 {OrderId}", 
                            responsibleNurseId, order.Id);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 未找到负责护士，操作医嘱 {OrderId} 的 NurseId 将保持为空", order.Id);
                    }
                }
                catch (Exception nurseEx)
                {
                    _logger.LogError(nurseEx, "❌ 计算负责护士失败，操作医嘱 {OrderId}", order.Id);
                    // 护士分配失败不影响医嘱创建，继续执行
                }
            }
            else
            {
                _logger.LogInformation("操作医嘱 {OrderId} 已指定护士: {NurseId}", order.Id, order.NurseId);
            }

            // 4. 自动生成执行任务（医嘱下达后立即拆分任务）
            try
            {
                _logger.LogInformation("开始为操作医嘱 {OrderId} 自动生成执行任务", order.Id);
                var generatedTasks = await _taskService.GenerateExecutionTasksAsync(order);
                _logger.LogInformation("✅ 已为操作医嘱 {OrderId} 自动生成 {Count} 个执行任务", order.Id, generatedTasks.Count);
            }
            catch (Exception taskEx)
            {
                _logger.LogError(taskEx, "❌ 自动生成执行任务失败，操作医嘱 {OrderId}，但不影响医嘱创建", order.Id);
                // 任务生成失败不影响医嘱创建，继续执行
            }

            return order;
        }
    }
}

