using CareFlow.Application.DTOs.OrderAcknowledgement;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MedicalOrderEntity = CareFlow.Core.Models.Medical.MedicalOrder;

namespace CareFlow.Application.Services.OrderAcknowledgement;

/// <summary>
/// 医嘱签收服务实现
/// </summary>
public class OrderAcknowledgementService : IOrderAcknowledgementService
{
    private readonly IRepository<MedicalOrderEntity, long> _orderRepository;
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<Doctor, string> _doctorRepository;
    private readonly IRepository<Drug, string> _drugRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
    private readonly IRepository<MedicationReturnRequest, long> _returnRequestRepository;
    private readonly IMedicationOrderTaskService _medicationTaskService;
    private readonly IInspectionService _inspectionTaskService;
    private readonly ISurgicalOrderTaskService _surgicalTaskService;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<OrderAcknowledgementService> _logger;

    public OrderAcknowledgementService(
        IRepository<MedicalOrderEntity, long> orderRepository,
        IRepository<Patient, string> patientRepository,
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<Doctor, string> doctorRepository,
        IRepository<Drug, string> drugRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        IRepository<BarcodeIndex, string> barcodeRepository,
        IRepository<MedicationReturnRequest, long> returnRequestRepository,
        IMedicationOrderTaskService medicationTaskService,
        IInspectionService inspectionTaskService,
        ISurgicalOrderTaskService surgicalTaskService,
        INurseAssignmentService nurseAssignmentService,
        IBarcodeService barcodeService,
        ILogger<OrderAcknowledgementService> logger)
    {
        _orderRepository = orderRepository;
        _patientRepository = patientRepository;
        _taskRepository = taskRepository;
        _doctorRepository = doctorRepository;
        _drugRepository = drugRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _barcodeRepository = barcodeRepository;
        _returnRequestRepository = returnRequestRepository;
        _medicationTaskService = medicationTaskService;
        _inspectionTaskService = inspectionTaskService;
        _surgicalTaskService = surgicalTaskService;
        _nurseAssignmentService = nurseAssignmentService;
        _barcodeService = barcodeService;
        _logger = logger;
    }

    /// <summary>
    /// 获取科室所有患者的未签收医嘱统计
    /// </summary>
    public async Task<List<PatientUnacknowledgedSummaryDto>> GetPendingOrdersSummaryAsync(string deptCode)
    {
        _logger.LogInformation("========== 获取科室 {DeptCode} 患者未签收医嘱统计 ==========", deptCode);

        try
        {
            // 1. 查询该科室所有在院患者
            var patients = await _patientRepository.GetQueryable()
                .Include(p => p.Bed)
                    .ThenInclude(b => b.Ward)
                .Where(p => p.Bed.Ward.DepartmentId == deptCode && p.Status == "Active")
                .ToListAsync();

            _logger.LogInformation("科室患者总数: {Count}", patients.Count);

            var result = new List<PatientUnacknowledgedSummaryDto>();

            foreach (var patient in patients)
            {
                // 2. 统计该患者的未签收医嘱数量（状态为PendingReceive或PendingStop）
                // 注意：已退回（Rejected）的医嘱不计入待签收数量，等待医生修改后重新提交
                var unacknowledgedCount = await _orderRepository.GetQueryable()
                    .Where(o => o.PatientId == patient.Id && 
                               (o.Status == OrderStatus.PendingReceive || 
                                o.Status == OrderStatus.PendingStop))
                    .CountAsync();

                result.Add(new PatientUnacknowledgedSummaryDto
                {
                    PatientId = patient.Id,
                    PatientName = patient.Name,
                    BedId = patient.BedId,
                    Gender = patient.Gender,
                    Age = patient.Age,
                    Weight = patient.Weight,
                    NursingGrade = (int)patient.NursingGrade,
                    WardId = patient.Bed.WardId,
                    WardName = patient.Bed.Ward.Id,
                    UnacknowledgedCount = unacknowledgedCount
                });
            }

            _logger.LogInformation("✅ 成功获取 {Count} 个患者的统计信息", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取患者未签收医嘱统计失败");
            throw;
        }
    }

    /// <summary>
    /// 获取指定患者的待签收医嘱
    /// </summary>
    public async Task<PatientPendingOrdersDto> GetPatientPendingOrdersAsync(string patientId)
    {
        _logger.LogInformation("========== 获取患者 {PatientId} 待签收医嘱 ==========", patientId);

        try
        {
            // 查询所有待签收的医嘱（包括新开和停止）
            // 注意：已退回（Rejected）的医嘱不应再显示在护士列表中，等待医生修改后重新提交
            var pendingOrders = await _orderRepository.GetQueryable()
                .Include(o => o.Doctor)
                .Include(o => o.Patient)
                .Where(o => o.PatientId == patientId && 
                           (o.Status == OrderStatus.PendingReceive || 
                            o.Status == OrderStatus.PendingStop))
                .OrderByDescending(o => o.CreateTime)
                .ToListAsync();

            _logger.LogInformation("查询到 {Count} 条待签收医嘱", pendingOrders.Count);

            var result = new PatientPendingOrdersDto
            {
                NewOrders = new List<PendingOrderDto>(),
                StoppedOrders = new List<PendingOrderDto>()
            };

            foreach (var order in pendingOrders)
            {
                var dto = await MapToPendingOrderDto(order);

                // 根据状态分类
                if (order.Status == OrderStatus.PendingStop)
                {
                    result.StoppedOrders.Add(dto);
                }
                else
                {
                    result.NewOrders.Add(dto);
                }
            }

            _logger.LogInformation("✅ 新开医嘱: {NewCount}, 停止医嘱: {StoppedCount}", 
                result.NewOrders.Count, result.StoppedOrders.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取患者待签收医嘱失败");
            throw;
        }
    }

    /// <summary>
    /// 批量签收医嘱
    /// </summary>
    public async Task<AcknowledgeOrdersResponseDto> AcknowledgeOrdersAsync(
        AcknowledgeOrdersRequestDto request)
    {
        _logger.LogInformation("========== 开始批量签收医嘱 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 医嘱数量: {Count}", 
            request.NurseId, request.OrderIds.Count);

        var results = new List<AcknowledgedOrderResultDto>();
        var errors = new List<string>();

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                _logger.LogInformation("--- 处理医嘱 {OrderId} ---", orderId);

                // 1. 查询医嘱并验证状态
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    var error = $"医嘱 {orderId} 不存在";
                    _logger.LogWarning(error);
                    errors.Add(error);
                    continue;
                }

                if (order.Status != OrderStatus.PendingReceive && 
                    order.Status != OrderStatus.Rejected && 
                    order.Status != OrderStatus.PendingStop)
                {
                    var error = $"医嘱 {orderId} 状态为 {order.Status}，不允许签收";
                    _logger.LogWarning(error);
                    errors.Add(error);
                    continue;
                }

                AcknowledgedOrderResultDto result;

                // 2. 根据状态判断是新开签收还是停止签收
                if (order.Status == OrderStatus.PendingStop)
                {
                    result = await AcknowledgeStoppedOrderAsync(order, request.NurseId);
                }
                else
                {
                    result = await AcknowledgeNewOrderAsync(order, request.NurseId);
                }

                results.Add(result);
                _logger.LogInformation("✅ 医嘱 {OrderId} 签收成功", orderId);
            }
            catch (Exception ex)
            {
                var error = $"签收医嘱 {orderId} 失败: {ex.Message}";
                _logger.LogError(ex, error);
                errors.Add(error);
            }
        }

        var response = new AcknowledgeOrdersResponseDto
        {
            Success = results.Count > 0,
            Message = errors.Count > 0
                ? $"成功签收 {results.Count} 条，失败 {errors.Count} 条"
                : $"成功签收 {results.Count} 条医嘱",
            Results = results,
            Errors = errors.Count > 0 ? errors : null
        };

        _logger.LogInformation("========== 批量签收完成：成功 {Success}，失败 {Failed} ==========",
            results.Count, errors.Count);

        return response;
    }

    /// <summary>
    /// 退回医嘱
    /// </summary>
    public async Task<RejectOrdersResponseDto> RejectOrdersAsync(RejectOrdersRequestDto request)
    {
        _logger.LogInformation("========== 开始批量退回医嘱 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 医嘱数量: {Count}, 原因: {Reason}",
            request.NurseId, request.OrderIds.Count, request.RejectReason);

        var rejectedIds = new List<long>();
        var errors = new List<string>();

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    errors.Add($"医嘱 {orderId} 不存在");
                    continue;
                }

                if (order.Status != OrderStatus.PendingReceive)
                {
                    errors.Add($"医嘱 {orderId} 状态为 {order.Status}，只能退回 PendingReceive 状态的医嘱");
                    continue;
                }

                var previousStatus = order.Status;
                
                // 更新状态为Rejected，让医生重新修改
                order.Status = OrderStatus.Rejected;
                order.NurseId = request.NurseId;
                order.RejectReason = request.RejectReason;
                order.RejectedAt = DateTime.UtcNow;
                order.RejectedByNurseId = request.NurseId;
                
                await _orderRepository.UpdateAsync(order);
                
                // 插入状态历史记录
                var history = new MedicalOrderStatusHistory
                {
                    MedicalOrderId = order.Id,
                    FromStatus = previousStatus,
                    ToStatus = OrderStatus.Rejected,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = request.NurseId,
                    ChangedByType = "Nurse",
                    Reason = request.RejectReason
                };
                await _statusHistoryRepository.AddAsync(history);
                
                rejectedIds.Add(orderId);

                _logger.LogInformation("✅ 医嘱 {OrderId} 已退回", orderId);
            }
            catch (Exception ex)
            {
                var error = $"退回医嘱 {orderId} 失败: {ex.Message}";
                _logger.LogError(ex, error);
                errors.Add(error);
            }
        }

        return new RejectOrdersResponseDto
        {
            Success = rejectedIds.Count > 0,
            Message = errors.Count > 0
                ? $"成功退回 {rejectedIds.Count} 条，失败 {errors.Count} 条"
                : $"成功退回 {rejectedIds.Count} 条医嘱",
            RejectedOrderIds = rejectedIds,
            Errors = errors.Count > 0 ? errors : null
        };
    }

    /// <summary>
    /// 护士拒绝停嘱
    /// 医嘱状态: PendingStop → 停止前的原始状态（通过历史记录查询）
    /// 任务状态: OrderStopping → 锁定前的原始状态
    /// </summary>
    public async Task<RejectStopOrderResponseDto> RejectStopOrderAsync(
        RejectStopOrderRequestDto request)
    {
        _logger.LogInformation("========== 开始批量拒绝停嘱 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 医嘱数量: {Count}, 原因: {Reason}",
            request.NurseId, request.OrderIds.Count, request.RejectReason);

        var rejectedOrderIds = new List<long>();
        var restoredTaskIds = new List<long>();
        var taskRestorationDetails = new Dictionary<long, string>();
        var errors = new List<string>();

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                _logger.LogInformation("--- 处理医嘱 {OrderId} 拒绝停嘱 ---", orderId);

                // 1. 验证医嘱状态
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    var error = $"医嘱 {orderId} 不存在";
                    _logger.LogWarning(error);
                    errors.Add(error);
                    continue;
                }

                if (order.Status != OrderStatus.PendingStop)
                {
                    var error = $"医嘱 {orderId} 状态为 {order.Status}，只能拒绝 PendingStop 状态的医嘱";
                    _logger.LogWarning(error);
                    errors.Add(error);
                    continue;
                }

                var currentStatus = order.Status;
                
                // 2. 查询历史记录表，获取变为 PendingStop 之前的状态
                var lastHistory = await _statusHistoryRepository.GetQueryable()
                    .Where(h => h.MedicalOrderId == order.Id && h.ToStatus == OrderStatus.PendingStop)
                    .OrderByDescending(h => h.ChangedAt)
                    .FirstOrDefaultAsync();
                
                OrderStatus statusToRestore;
                if (lastHistory != null)
                {
                    statusToRestore = lastHistory.FromStatus;
                    _logger.LogInformation("从历史记录获取停止前状态: {FromStatus}", statusToRestore);
                }
                else
                {
                    // 如果没有找到历史记录，默认恢复为 InProgress
                    statusToRestore = OrderStatus.InProgress;
                    _logger.LogWarning("未找到医嘱 {OrderId} 的停止前状态历史记录，默认恢复为 InProgress", orderId);
                }
                
                // 3. 恢复医嘱状态到停止前的状态
                order.Status = statusToRestore;
                order.StopRejectReason = request.RejectReason;
                order.StopRejectedAt = DateTime.UtcNow;
                order.StopRejectedByNurseId = request.NurseId;
                
                // 清空停嘱相关字段（医生可能会再次下达停嘱）
                order.StopReason = null;
                order.StopOrderTime = null;
                order.StopDoctorId = null;
                order.StopConfirmedAt = null;
                order.StopConfirmedByNurseId = null;
                
                await _orderRepository.UpdateAsync(order);
                
                _logger.LogInformation("✅ 医嘱 {OrderId} 状态已从 PendingStop 恢复为 {RestoredStatus}", 
                    orderId, statusToRestore);
                
                // 4. 插入状态历史记录
                var history = new MedicalOrderStatusHistory
                {
                    MedicalOrderId = order.Id,
                    FromStatus = currentStatus,
                    ToStatus = statusToRestore,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = request.NurseId,
                    ChangedByType = "Nurse",
                    Reason = $"护士拒绝停嘱: {request.RejectReason}"
                };
                await _statusHistoryRepository.AddAsync(history);
                
                // 4. 查找并恢复被锁定的任务
                var lockedTasks = await _taskRepository.ListAsync(t =>
                    t.MedicalOrderId == order.Id &&
                    t.Status == ExecutionTaskStatus.OrderStopping);
                
                _logger.LogInformation("医嘱 {OrderId} 有 {Count} 个被锁定的任务需要恢复", 
                    orderId, lockedTasks.Count);
                
                foreach (var task in lockedTasks)
                {
                    // ✅ 关键逻辑：恢复到锁定前的状态
                    var restoredStatus = task.StatusBeforeLocking ?? ExecutionTaskStatus.Pending;
                    var originalStatus = task.Status;
                    
                    task.Status = restoredStatus;
                    task.StatusBeforeLocking = null; // 清空锁定前状态字段
                    task.LastModifiedAt = DateTime.UtcNow;
                    
                    // 记录操作日志到 ExceptionReason（用于审计）
                    var operationLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 护士 {request.NurseId} 拒绝停嘱，" +
                                      $"任务从 {originalStatus} 恢复为 {restoredStatus}。" +
                                      $"原因: {request.RejectReason}";
                    
                    task.ExceptionReason = string.IsNullOrEmpty(task.ExceptionReason) 
                        ? operationLog 
                        : task.ExceptionReason + "\n" + operationLog;
                    
                    await _taskRepository.UpdateAsync(task);
                    
                    restoredTaskIds.Add(task.Id);
                    taskRestorationDetails[task.Id] = restoredStatus.ToString();
                    
                    _logger.LogInformation("✅ 任务 {TaskId} 已从 OrderStopping 恢复为 {Status}", 
                        task.Id, restoredStatus);
                }
                
                rejectedOrderIds.Add(orderId);
                
                _logger.LogInformation("✅ 医嘱 {OrderId} 停嘱已拒绝，{TaskCount} 个任务已解锁", 
                    orderId, lockedTasks.Count);
            }
            catch (Exception ex)
            {
                var error = $"拒绝停嘱 {orderId} 失败: {ex.Message}";
                _logger.LogError(ex, error);
                errors.Add(error);
            }
        }

        var response = new RejectStopOrderResponseDto
        {
            Success = rejectedOrderIds.Count > 0,
            Message = errors.Count > 0
                ? $"成功拒绝 {rejectedOrderIds.Count} 条停嘱，失败 {errors.Count} 条"
                : $"成功拒绝 {rejectedOrderIds.Count} 条停嘱，恢复 {restoredTaskIds.Count} 个任务",
            RejectedOrderIds = rejectedOrderIds,
            RestoredTaskIds = restoredTaskIds,
            TaskRestorationDetails = taskRestorationDetails,
            Errors = errors.Count > 0 ? errors : null
        };

        _logger.LogInformation("========== 批量拒绝停嘱完成：成功 {Success}，失败 {Failed}，恢复任务 {Restored} ==========",
            rejectedOrderIds.Count, errors.Count, restoredTaskIds.Count);

        return response;
    }

    // ==================== 私有辅助方法 ====================

    /// <summary>
    /// 签收新开医嘱（核心逻辑）
    /// </summary>
    private async Task<AcknowledgedOrderResultDto> AcknowledgeNewOrderAsync(
        MedicalOrderEntity order, string nurseId)
    {
        _logger.LogInformation("签收新开医嘱，类型: {OrderType}", order.OrderType);

        var previousStatus = order.Status;
        
        // 1. 更新医嘱状态
        order.Status = OrderStatus.Accepted;
        order.NurseId = nurseId;
        order.SignedAt = DateTime.UtcNow;
        order.SignedByNurseId = nurseId;
        
        await _orderRepository.UpdateAsync(order);
        
        // 插入状态历史记录
        var history = new MedicalOrderStatusHistory
        {
            MedicalOrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = OrderStatus.Accepted,
            ChangedAt = DateTime.UtcNow,
            ChangedById = nurseId,
            ChangedByType = "Nurse",
            Reason = "护士签收"
        };
        await _statusHistoryRepository.AddAsync(history);

        // 2. 调用对应类型的任务拆分服务
        List<ExecutionTask> tasks = await GenerateTasksForOrderAsync(order);

        // 3. 为每个任务计算责任护士
        int assignedCount = 0;
        int unassignedCount = 0;

        foreach (var task in tasks)
        {
            var responsibleNurse = await _nurseAssignmentService
                .CalculateResponsibleNurseAsync(order.PatientId, task.PlannedStartTime);

            if (responsibleNurse != null)
            {
                task.AssignedNurseId = responsibleNurse;
                assignedCount++;
                _logger.LogInformation("任务 {TaskId} 分配计划责任护士 {NurseId}", task.Id, responsibleNurse);
            }
            else
            {
                unassignedCount++;
                _logger.LogWarning("任务 {TaskId} 计划时间 {Time} 无排班护士，计划责任护士留空",
                    task.Id, task.PlannedStartTime);
            }

            await _taskRepository.UpdateAsync(task);
        }

        // 4. 为每个任务生成条形码索引和图片
        int barcodeSuccessCount = 0;
        int barcodeFailCount = 0;
        
        foreach (var task in tasks)
        {
            try
            {
                await GenerateBarcodeForTaskAsync(task);
                barcodeSuccessCount++;
            }
            catch (Exception ex)
            {
                barcodeFailCount++;
                _logger.LogError(ex, "为任务 {TaskId} 生成条形码失败", task.Id);
                // 条形码生成失败不应阻断签收流程
            }
        }
        
        _logger.LogInformation("条形码生成完成: 成功 {Success}, 失败 {Failed}", 
            barcodeSuccessCount, barcodeFailCount);

        // 5. 检查今天是否有任务需要执行
        var today = DateTime.Today;
        var todayTasks = tasks.Where(t => t.PlannedStartTime.Date == today).ToList();

        var result = new AcknowledgedOrderResultDto
        {
            OrderId = order.Id,
            OrderType = order.OrderType,
            GeneratedTaskIds = tasks.Select(t => t.Id).ToList(),
            NeedTodayAction = todayTasks.Any(),
            TaskSummary = new TaskGenerationSummary
            {
                TotalTaskCount = tasks.Count,
                TodayTaskCount = todayTasks.Count,
                AssignedTaskCount = assignedCount,
                UnassignedTaskCount = unassignedCount
            }
        };

        // 6. 判断需要的操作类型
        result.ActionType = DetermineActionType(order, todayTasks);

        _logger.LogInformation("任务生成完成: 总计 {Total}, 今日 {Today}, 已分配 {Assigned}, 未分配 {Unassigned}",
            tasks.Count, todayTasks.Count, assignedCount, unassignedCount);

        return result;
    }

    /// <summary>
    /// 签收停止医嘱
    /// 医嘱状态: PendingStop → Stopped
    /// 任务状态: OrderStopping → Stopped
    /// </summary>
    private async Task<AcknowledgedOrderResultDto> AcknowledgeStoppedOrderAsync(
        MedicalOrderEntity order, string nurseId)
    {
        _logger.LogInformation("========== 签收停止医嘱 ==========");
        _logger.LogInformation("医嘱ID: {OrderId}, 护士ID: {NurseId}", order.Id, nurseId);

        var previousStatus = order.Status;
        
        // 1. 更新医嘱状态为 Stopped
        order.Status = OrderStatus.Stopped;
        order.NurseId = nurseId;
        order.StopConfirmedAt = DateTime.UtcNow;
        order.StopConfirmedByNurseId = nurseId;
        
        await _orderRepository.UpdateAsync(order);
        
        _logger.LogInformation("✅ 医嘱 {OrderId} 状态已从 PendingStop 更新为 Stopped", order.Id);
        
        // 2. 插入状态历史记录
        var history = new MedicalOrderStatusHistory
        {
            MedicalOrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = OrderStatus.Stopped,
            ChangedAt = DateTime.UtcNow,
            ChangedById = nurseId,
            ChangedByType = "Nurse",
            Reason = "护士确认停嘱"
        };
        await _statusHistoryRepository.AddAsync(history);

        // 3. ✅ 核心修复：查找所有被锁定的任务（OrderStopping 状态）
        var lockedTasks = await _taskRepository.ListAsync(t =>
            t.MedicalOrderId == order.Id &&
            t.Status == ExecutionTaskStatus.OrderStopping);

        _logger.LogInformation("该停止医嘱有 {Count} 个锁定任务需要处理", lockedTasks.Count);

        var stoppedTaskIds = new List<long>();
        var pendingReturnTaskIds = new List<long>();
        
        // 4. ✅ 处理所有锁定的任务
        foreach (var task in lockedTasks)
        {
            var originalStatus = task.Status;
            var statusBeforeLocking = task.StatusBeforeLocking;
            
            // 🆕 检查是否需要退药
            if (statusBeforeLocking == ExecutionTaskStatus.Applied ||
                statusBeforeLocking == ExecutionTaskStatus.AppliedConfirmed)
            {
                // 创建退药申请记录
                var returnRequest = new MedicationReturnRequest
                {
                    ExecutionTaskId = task.Id,
                    ReturnType = "OrderStopped",
                    RequestedBy = nurseId,
                    RequestedAt = DateTime.UtcNow,
                    Reason = $"医嘱停止：{order.StopReason}",
                    Status = "Pending"
                };
                await _returnRequestRepository.AddAsync(returnRequest);
                
                // 🆕 任务状态改为待退药（而不是直接Stopped）
                task.Status = ExecutionTaskStatus.PendingReturn;
                task.StatusBeforeLocking = null;
                task.LastModifiedAt = DateTime.UtcNow;
                
                await _taskRepository.UpdateAsync(task);
                pendingReturnTaskIds.Add(task.Id);
                
                _logger.LogInformation("✅ 任务 {TaskId} 需退药，状态: OrderStopping → PendingReturn (原状态: {StatusBeforeLocking})",
                    task.Id, statusBeforeLocking.ToString());
            }
            else
            {
                // 其他状态直接改为Stopped
                task.Status = ExecutionTaskStatus.Stopped;
                task.StatusBeforeLocking = null;
                task.ActualEndTime = DateTime.UtcNow;
                task.LastModifiedAt = DateTime.UtcNow;
                
                // 记录详细的停止原因
                var stopReason = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 医嘱已停止（护士 {nurseId} 确认）。" +
                               $"任务原状态: {statusBeforeLocking?.ToString() ?? "未记录"}，" +
                               $"锁定状态: {originalStatus}";
                
                task.ExceptionReason = string.IsNullOrEmpty(task.ExceptionReason) 
                    ? stopReason 
                    : task.ExceptionReason + "\n" + stopReason;
                
                await _taskRepository.UpdateAsync(task);
                stoppedTaskIds.Add(task.Id);
                
                _logger.LogInformation("✅ 任务 {TaskId} 已从 OrderStopping 变更为 Stopped (原状态: {StatusBeforeLocking})",
                    task.Id, statusBeforeLocking?.ToString() ?? "未记录");
            }
        }
        
        if (pendingReturnTaskIds.Count > 0)
        {
            _logger.LogInformation("⚠️ 医嘱 {OrderId} 有 {Count} 个任务需要护士在药品申请界面确认退药",
                order.Id, pendingReturnTaskIds.Count);
        }

        // 5. ✅ 检查是否有已提交但未确认的申请（需要通知外部系统取消）
        var appliedTasks = await _taskRepository.ListAsync(t =>
            t.MedicalOrderId == order.Id &&
            (t.Status == ExecutionTaskStatus.Applied || 
             t.Status == ExecutionTaskStatus.AppliedConfirmed));
             
        var pendingRequestIds = appliedTasks.Select(t => t.Id).ToList();
        var hasPendingRequests = pendingRequestIds.Any();
        
        if (hasPendingRequests)
        {
            _logger.LogWarning("⚠️ 医嘱 {OrderId} 有 {Count} 个已提交申请（状态: Applied/AppliedConfirmed），" +
                             "建议通知药房/检查站取消。任务ID: {TaskIds}",
                order.Id, pendingRequestIds.Count, string.Join(", ", pendingRequestIds));
        }
        
        // 6. ✅ 统计所有任务状态（用于完整性检查）
        var allTasks = await _taskRepository.ListAsync(t => t.MedicalOrderId == order.Id);
        var taskStatusSummary = allTasks
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
        
        _logger.LogInformation("医嘱 {OrderId} 所有任务状态统计: {Summary}", 
            order.Id, string.Join(", ", taskStatusSummary.Select(kv => $"{kv.Key}={kv.Value}")));

        var result = new AcknowledgedOrderResultDto
        {
            OrderId = order.Id,
            OrderType = order.OrderType,
            NeedTodayAction = false,
            ActionType = "None",
            GeneratedTaskIds = stoppedTaskIds,
            HasPendingRequests = hasPendingRequests,
            PendingRequestIds = pendingRequestIds
        };

        _logger.LogInformation("========== 停止医嘱签收完成：作废 {StoppedCount} 个任务，" +
                             "待取消申请 {PendingCount} 个 ==========",
            stoppedTaskIds.Count, pendingRequestIds.Count);

        return result;
    }

    /// <summary>
    /// 根据医嘱类型调用对应的任务生成服务
    /// </summary>
    private async Task<List<ExecutionTask>> GenerateTasksForOrderAsync(MedicalOrderEntity order)
    {
        List<ExecutionTask> tasks = new();

        try
        {
            if (order is MedicationOrder medicationOrder)
            {
                tasks = await _medicationTaskService.GenerateExecutionTasksAsync(medicationOrder);
                _logger.LogInformation("药品医嘱生成 {Count} 个任务", tasks.Count);
            }
            else if (order is InspectionOrder inspectionOrder)
            {
                // 检查医嘱签收时生成申请任务，预约确认后生成执行任务
                var applicationTask = await _inspectionTaskService.GenerateApplicationTaskAsync(inspectionOrder);
                tasks = new List<ExecutionTask> { applicationTask };
                _logger.LogInformation("检查医嘱生成1个申请任务，预约确认后将生成执行任务");
            }
            else if (order is SurgicalOrder surgicalOrder)
            {
                tasks = await _surgicalTaskService.GenerateExecutionTasksAsync(surgicalOrder);
                _logger.LogInformation("手术医嘱生成 {Count} 个任务", tasks.Count);
            }
            else if (order is OperationOrder operationOrder)
            {
                // TODO: 阶段四实现 - 操作医嘱任务生成
                _logger.LogWarning("操作医嘱任务生成尚未实现，医嘱ID: {OrderId}", order.Id);
            }
            else
            {
                _logger.LogWarning("未知的医嘱类型: {OrderType}", order.OrderType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成任务时发生异常，医嘱ID: {OrderId}", order.Id);
            throw;
        }

        return tasks;
    }

    /// <summary>
    /// 判断需要的操作类型
    /// </summary>
    private string DetermineActionType(MedicalOrderEntity order, List<ExecutionTask> todayTasks)
    {
        if (!todayTasks.Any())
        {
            return "None";
        }

        // 药品医嘱或手术医嘱：检查今天是否有取药任务
        if (order is MedicationOrder || order is SurgicalOrder)
        {
            var hasTodayRetrieve = todayTasks.Any(t =>
                t.Category == TaskCategory.Verification &&
                t.DataPayload.Contains("RetrieveMedication"));

            if (hasTodayRetrieve)
            {
                return "RequestMedication";
            }
        }
        // 检查医嘱
        else if (order is InspectionOrder)
        {
            return "RequestInspection";
        }

        return "None";
    }

    /// <summary>
    /// 将医嘱实体映射为待签收DTO
    /// </summary>
    private async Task<PendingOrderDto> MapToPendingOrderDto(MedicalOrderEntity order)
    {
        var dto = new PendingOrderDto
        {
            OrderId = order.Id,
            OrderType = order.OrderType,
            IsLongTerm = order.IsLongTerm,
            CreateTime = order.CreateTime,
            DoctorName = order.Doctor?.Name ?? "未知医生",
            DoctorId = order.DoctorId,
            PlantEndTime = order.PlantEndTime,
            Remarks = order.Remarks
        };

        // 根据不同类型填充特定字段
        if (order is MedicationOrder medicationOrder)
        {
            dto.DisplayText = await BuildMedicationOrderDisplayText(medicationOrder);
            dto.UsageRoute = medicationOrder.UsageRoute.ToString();
            dto.TimingStrategy = medicationOrder.TimingStrategy;
            dto.StartTime = medicationOrder.StartTime;
            dto.Items = await LoadMedicationItems(order.Id);
        }
        else if (order is InspectionOrder inspectionOrder)
        {
            dto.DisplayText = $"检查项目: {inspectionOrder.ItemCode}";
            dto.ItemCode = inspectionOrder.ItemCode;
            dto.Location = inspectionOrder.Location;
        }
        else if (order is SurgicalOrder surgicalOrder)
        {
            dto.DisplayText = $"手术: {surgicalOrder.SurgeryName}";
            dto.SurgeryName = surgicalOrder.SurgeryName;
            dto.ScheduleTime = surgicalOrder.ScheduleTime;
            dto.AnesthesiaType = surgicalOrder.AnesthesiaType;
            dto.Items = await LoadMedicationItems(order.Id);
        }
        else if (order is OperationOrder operationOrder)
        {
            dto.DisplayText = $"操作: {operationOrder.OpId}";
            dto.OpId = operationOrder.OpId;
        }

        // 停止医嘱特有字段（PendingStop表示待护士签收停止）
        if (order.Status == OrderStatus.PendingStop)
        {
            dto.StopTime = order.EndTime;
            dto.StopReason = order.StopReason ?? "医生停止";
        }

        // 退回医嘱特有字段（Rejected表示护士已退回）
        if (order.Status == OrderStatus.Rejected)
        {
            dto.RejectReason = order.RejectReason;
            dto.RejectTime = order.RejectedAt;
        }

        return dto;
    }

    /// <summary>
    /// 构建药品医嘱显示文本
    /// </summary>
    private async Task<string> BuildMedicationOrderDisplayText(MedicationOrder order)
    {
        var items = await LoadMedicationItems(order.Id);
        if (items.Count == 0)
        {
            return "药品医嘱（无药品信息）";
        }

        if (items.Count == 1)
        {
            var item = items[0];
            return $"{item.DrugName} {item.Dosage}";
        }

        // 多个药品
        return $"{items[0].DrugName} 等 {items.Count} 种药品";
    }

    /// <summary>
    /// 加载医嘱药品明细
    /// </summary>
    private async Task<List<OrderItemDto>> LoadMedicationItems(long orderId)
    {
        var order = await _orderRepository.GetQueryable()
            .Include(o => o.Items)
                .ThenInclude(i => i.Drug)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order?.Items == null || !order.Items.Any())
        {
            return new List<OrderItemDto>();
        }

        return order.Items.Select(item => new OrderItemDto
        {
            DrugId = item.DrugId,
            DrugName = item.Drug?.GenericName ?? "未知药品",
            Specification = item.Drug?.Specification ?? "",
            Dosage = item.Dosage,
            Note = item.Note
        }).ToList();
    }

    /// <summary>
    /// 为任务生成条形码索引和图片（签收医嘱时调用）
    /// </summary>
    public async Task GenerateBarcodeForTaskAsync(ExecutionTask task)
    {
        try
        {
            var barcodeIndex = new BarcodeIndex
            {
                Id = $"ExecutionTasks-{task.Id}",
                TableName = "ExecutionTasks",
                RecordId = task.Id.ToString()
            };

            // 生成条形码并保存到文件系统
            var barcodeResult = await _barcodeService.GenerateAndSaveBarcodeAsync(barcodeIndex, saveToFile: true);
            
            // 更新条形码索引信息
            barcodeIndex.ImagePath = barcodeResult.FilePath;
            barcodeIndex.ImageSize = barcodeResult.FileSize;
            barcodeIndex.ImageMimeType = barcodeResult.MimeType;
            barcodeIndex.ImageGeneratedAt = barcodeResult.GeneratedAt;

            // 保存条形码索引到数据库
            await _barcodeRepository.AddAsync(barcodeIndex);
            
            _logger.LogDebug("已为ExecutionTask {TaskId} 生成条形码索引和图片文件 {FilePath}", 
                task.Id, barcodeResult.FilePath ?? "内存中");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为ExecutionTask {TaskId} 生成条形码时发生错误", task.Id);
            throw; // 重新抛出异常，让调用方处理
        }
    }
}
