using CareFlow.Application.Interfaces;
using CareFlow.Application.Options;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareFlow.Application.Services.Scheduling;

/// <summary>
/// 交班任务转移服务
/// 负责在交班时将未完成的任务和未签收的医嘱转移给新班次的护士
/// </summary>
public class ShiftHandoverService
{
    private readonly IRepository<NursingTask, long> _nursingTaskRepo;
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> _medicalOrderRepo;
    private readonly IRepository<ExecutionTask, long> _executionTaskRepo;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly NursingScheduleOptions _options;
    private readonly ILogger<ShiftHandoverService> _logger;
    private readonly TimeZoneInfo _chinaTimeZone;

    public ShiftHandoverService(
        IRepository<NursingTask, long> nursingTaskRepo,
        IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> medicalOrderRepo,
        IRepository<ExecutionTask, long> executionTaskRepo,
        INurseAssignmentService nurseAssignmentService,
        IOptions<NursingScheduleOptions> options,
        ILogger<ShiftHandoverService> logger)
    {
        _nursingTaskRepo = nursingTaskRepo;
        _medicalOrderRepo = medicalOrderRepo;
        _executionTaskRepo = executionTaskRepo;
        _nurseAssignmentService = nurseAssignmentService;
        _options = options.Value;
        _logger = logger;
        _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
    }

    /// <summary>
    /// 执行交班任务转移
    /// </summary>
    public async Task TransferUnfinishedTasksAsync()
    {
        try
        {
            var nowInChina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chinaTimeZone);
            _logger.LogInformation("🔄 开始执行交班任务转移 @ {Time}", nowInChina.ToString("HH:mm"));

            // 统计总数
            var medicalOrderTransferred = await TransferUnacknowledgedOrdersAsync();
            var executionTaskTransferred = await TransferUnfinishedExecutionTasksAsync();
            var nursingTaskTransferred = await TransferUnfinishedNursingTasksAsync();

            _logger.LogInformation("✅ 交班完成: 医嘱转移={MO}, 执行任务转移={ET}, 护理任务转移={NT}", 
                medicalOrderTransferred, executionTaskTransferred, nursingTaskTransferred);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 交班任务转移失败");
            throw;
        }
    }

    /// <summary>
    /// 转移未签收的医疗订单
    /// 逻辑：所有未签收医嘱（Status = PendingReceive）会按照排班表重新分配签收护士（NurseId）
    /// </summary>
    private async Task<int> TransferUnacknowledgedOrdersAsync()
    {
        try
        {
            _logger.LogInformation("📋 开始处理未签收医疗订单...");

            // 查询所有未签收的医疗订单
            var unacknowledgedOrders = await _medicalOrderRepo.GetQueryable()
                .Where(o => o.Status == OrderStatus.PendingReceive)
                .ToListAsync();

            if (unacknowledgedOrders.Count == 0)
            {
                _logger.LogInformation("ℹ️ 没有未签收的医疗订单");
                return 0;
            }

            _logger.LogInformation("📋 查询到 {Count} 条未签收医疗订单", unacknowledgedOrders.Count);

            var transferredCount = 0;
            var failedCount = 0;

            // 为每个医嘱重新计算签收护士
            foreach (var order in unacknowledgedOrders)
            {
                var oldNurseId = order.NurseId;

                // 使用当前时间重新计算负责护士
                var newNurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(order.PatientId, DateTime.UtcNow);

                if (newNurseId == null)
                {
                    failedCount++;
                    _logger.LogWarning("⚠️ 无法为医嘱分配新班次护士: OrderId={OrderId}, PatientId={PatientId}", 
                        order.Id, order.PatientId);
                    
                    // 未找到护士，保持未分配状态
                    order.NurseId = null;
                    continue;
                }

                // 如果护士没有变化，跳过更新
                if (newNurseId == oldNurseId)
                {
                    continue;
                }

                // 更新签收护士
                order.NurseId = newNurseId;
                await _medicalOrderRepo.UpdateAsync(order);
                transferredCount++;

                _logger.LogDebug("🔀 医嘱转移: OrderId={OrderId}, {OldNurse} → {NewNurse}", 
                    order.Id, oldNurseId ?? "未分配", newNurseId);
            }

            if (failedCount > 0)
            {
                // 将未能分配的医嘱更新为未分配状态
                foreach (var order in unacknowledgedOrders.Where(o => o.NurseId == null && o.Status == OrderStatus.PendingReceive))
                {
                    await _medicalOrderRepo.UpdateAsync(order);
                }
            }

            _logger.LogInformation("✅ 医疗订单处理完成: 转移={Transferred}, 失败={Failed}", 
                transferredCount, failedCount);

            return transferredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 未签收医疗订单转移失败");
            throw;
        }
    }

    /// <summary>
    /// 转移未完成的执行任务
    /// 逻辑：所有未完成的执行任务（包括 Applying, Applied, AppliedConfirmed, Pending, InProgress）会按照排班表重新分配负责护士
    /// </summary>
    private async Task<int> TransferUnfinishedExecutionTasksAsync()
    {
        try
        {
            _logger.LogInformation("📋 开始处理未完成执行任务...");

            // 查询所有未完成的执行任务
            // 包括: Applying, Applied, AppliedConfirmed, Pending, InProgress
            var unfinishedStatuses = new[] 
            { 
                ExecutionTaskStatus.Applying,
                ExecutionTaskStatus.Applied,
                ExecutionTaskStatus.AppliedConfirmed,
                ExecutionTaskStatus.Pending,
                ExecutionTaskStatus.InProgress
            };

            var unfinishedTasks = await _executionTaskRepo.GetQueryable()
                .Where(t => unfinishedStatuses.Contains(t.Status))
                .ToListAsync();

            if (unfinishedTasks.Count == 0)
            {
                _logger.LogInformation("ℹ️ 没有未完成的执行任务");
                return 0;
            }

            _logger.LogInformation("📋 查询到 {Count} 条未完成执行任务", unfinishedTasks.Count);

            var transferredCount = 0;
            var failedCount = 0;

            // 为每个任务重新计算负责护士
            foreach (var task in unfinishedTasks)
            {
                var oldNurseId = task.AssignedNurseId;

                // 使用当前时间重新计算负责护士
                var newNurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(task.PatientId, DateTime.UtcNow);

                if (newNurseId == null)
                {
                    failedCount++;
                    _logger.LogWarning("⚠️ 无法为执行任务分配新班次护士: TaskId={TaskId}, PatientId={PatientId}, Status={Status}", 
                        task.Id, task.PatientId, task.Status);
                    
                    // 未找到护士，设置为未分配
                    task.AssignedNurseId = null;
                    continue;
                }

                // 如果护士没有变化，跳过更新
                if (newNurseId == oldNurseId)
                {
                    _logger.LogDebug("ℹ️ 执行任务护士未变化: TaskId={TaskId}, NurseId={NurseId}", 
                        task.Id, newNurseId);
                    continue;
                }

                // 更新负责护士
                task.AssignedNurseId = newNurseId;
                await _executionTaskRepo.UpdateAsync(task);
                transferredCount++;

                _logger.LogDebug("🔀 执行任务转移: TaskId={TaskId}, Status={Status}, {OldNurse} → {NewNurse}", 
                    task.Id, task.Status, oldNurseId ?? "未分配", newNurseId);
            }

            if (failedCount > 0)
            {
                // 将未能分配的任务更新为未分配状态
                foreach (var task in unfinishedTasks.Where(t => t.AssignedNurseId == null && unfinishedStatuses.Contains(t.Status)))
                {
                    await _executionTaskRepo.UpdateAsync(task);
                }
            }

            _logger.LogInformation("✅ 执行任务处理完成: 转移={Transferred}, 失败={Failed}", 
                transferredCount, failedCount);

            return transferredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 未完成执行任务转移失败");
            throw;
        }
    }

    /// <summary>
    /// 转移未完成的护理任务
    /// 逻辑：所有未完成的护理任务（Status = Pending 或 InProgress）会按照排班表重新分配负责护士
    /// </summary>
    private async Task<int> TransferUnfinishedNursingTasksAsync()
    {
        try
        {
            _logger.LogInformation("📋 开始处理未完成护理任务...");

            // 查询所有未完成的护理任务
            // 包括: Pending, InProgress
            var unfinishedStatuses = new[] 
            { 
                ExecutionTaskStatus.Pending,
                ExecutionTaskStatus.InProgress
            };

            var unfinishedTasks = await _nursingTaskRepo.GetQueryable()
                .Where(t => unfinishedStatuses.Contains(t.Status))
                .ToListAsync();

            if (unfinishedTasks.Count == 0)
            {
                _logger.LogInformation("ℹ️ 没有未完成的护理任务");
                return 0;
            }

            _logger.LogInformation("📋 查询到 {Count} 条未完成护理任务", unfinishedTasks.Count);

            var transferredCount = 0;
            var failedCount = 0;

            // 为每个任务重新计算负责护士
            foreach (var task in unfinishedTasks)
            {
                var oldNurseId = task.AssignedNurseId;

                // 使用当前时间重新计算负责护士
                var newNurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(task.PatientId, DateTime.UtcNow);

                if (newNurseId == null)
                {
                    failedCount++;
                    _logger.LogWarning("⚠️ 无法为护理任务分配新班次护士: TaskId={TaskId}, PatientId={PatientId}, Status={Status}", 
                        task.Id, task.PatientId, task.Status);
                    
                    // 未找到护士，设置为未分配
                    task.AssignedNurseId = null;
                    continue;
                }

                // 如果护士没有变化，跳过更新
                if (newNurseId == oldNurseId)
                {
                    _logger.LogDebug("ℹ️ 护理任务护士未变化: TaskId={TaskId}, NurseId={NurseId}", 
                        task.Id, newNurseId);
                    continue;
                }

                // 更新负责护士
                task.AssignedNurseId = newNurseId;
                await _nursingTaskRepo.UpdateAsync(task);
                transferredCount++;

                _logger.LogDebug("🔀 护理任务转移: TaskId={TaskId}, Status={Status}, {OldNurse} → {NewNurse}", 
                    task.Id, task.Status, oldNurseId ?? "未分配", newNurseId);
            }

            if (failedCount > 0)
            {
                // 将未能分配的任务更新为未分配状态
                foreach (var task in unfinishedTasks.Where(t => t.AssignedNurseId == null && unfinishedStatuses.Contains(t.Status)))
                {
                    await _nursingTaskRepo.UpdateAsync(task);
                }
            }

            _logger.LogInformation("✅ 护理任务处理完成: 转移={Transferred}, 失败={Failed}", 
                transferredCount, failedCount);

            return transferredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 未完成护理任务转移失败");
            throw;
        }
    }
}
