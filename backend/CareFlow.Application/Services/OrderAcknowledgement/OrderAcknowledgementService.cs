using CareFlow.Application.DTOs.OrderAcknowledgement;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
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
    private readonly IMedicationOrderTaskService _medicationTaskService;
    private readonly IInspectionService _inspectionTaskService;
    private readonly ISurgicalOrderTaskService _surgicalTaskService;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<OrderAcknowledgementService> _logger;

    public OrderAcknowledgementService(
        IRepository<MedicalOrderEntity, long> orderRepository,
        IRepository<Patient, string> patientRepository,
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<Doctor, string> doctorRepository,
        IRepository<Drug, string> drugRepository,
        IMedicationOrderTaskService medicationTaskService,
        IInspectionService inspectionTaskService,
        ISurgicalOrderTaskService surgicalTaskService,
        INurseAssignmentService nurseAssignmentService,
        ILogger<OrderAcknowledgementService> logger)
    {
        _orderRepository = orderRepository;
        _patientRepository = patientRepository;
        _taskRepository = taskRepository;
        _doctorRepository = doctorRepository;
        _drugRepository = drugRepository;
        _medicationTaskService = medicationTaskService;
        _inspectionTaskService = inspectionTaskService;
        _surgicalTaskService = surgicalTaskService;
        _nurseAssignmentService = nurseAssignmentService;
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
                // 2. 统计该患者的未签收医嘱数量（状态为PendingReview）
                var unacknowledgedCount = await _orderRepository.GetQueryable()
                    .Where(o => o.PatientId == patient.Id && o.Status == "PendingReview")
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
            var pendingOrders = await _orderRepository.GetQueryable()
                .Include(o => o.Doctor)
                .Include(o => o.Patient)
                .Where(o => o.PatientId == patientId && 
                           (o.Status == "PendingReview" || o.Status == "Stopped"))
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
                if (order.Status == "Stopped")
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

                if (order.Status != "PendingReview" && order.Status != "Stopped")
                {
                    var error = $"医嘱 {orderId} 状态为 {order.Status}，不允许签收";
                    _logger.LogWarning(error);
                    errors.Add(error);
                    continue;
                }

                AcknowledgedOrderResultDto result;

                // 2. 根据状态判断是新开签收还是停止签收
                if (order.Status == "Stopped")
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

                if (order.Status != "PendingReview")
                {
                    errors.Add($"医嘱 {orderId} 状态为 {order.Status}，不允许退回");
                    continue;
                }

                // 更新状态为Draft，让医生重新修改
                order.Status = nameof(OrderStatus.Draft);
                order.NurseId = request.NurseId;
                // TODO: 需要在MedicalOrder实体中添加RejectReason字段
                // order.RejectReason = request.RejectReason;
                
                await _orderRepository.UpdateAsync(order);
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

    // ==================== 私有辅助方法 ====================

    /// <summary>
    /// 签收新开医嘱（核心逻辑）
    /// </summary>
    private async Task<AcknowledgedOrderResultDto> AcknowledgeNewOrderAsync(
        MedicalOrderEntity order, string nurseId)
    {
        _logger.LogInformation("签收新开医嘱，类型: {OrderType}", order.OrderType);

        // 1. 更新医嘱状态
        order.Status = "Accepted";
        order.NurseId = nurseId;
        await _orderRepository.UpdateAsync(order);

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
                task.ExecutorStaffId = responsibleNurse;
                assignedCount++;
                _logger.LogInformation("任务 {TaskId} 分配给护士 {NurseId}", task.Id, responsibleNurse);
            }
            else
            {
                unassignedCount++;
                _logger.LogWarning("任务 {TaskId} 计划时间 {Time} 无排班护士，责任护士留空",
                    task.Id, task.PlannedStartTime);
            }

            await _taskRepository.UpdateAsync(task);
        }

        // 4. 检查今天是否有任务需要执行
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

        // 5. 判断需要的操作类型
        result.ActionType = DetermineActionType(order, todayTasks);

        _logger.LogInformation("任务生成完成: 总计 {Total}, 今日 {Today}, 已分配 {Assigned}, 未分配 {Unassigned}",
            tasks.Count, todayTasks.Count, assignedCount, unassignedCount);

        return result;
    }

    /// <summary>
    /// 签收停止医嘱
    /// </summary>
    private async Task<AcknowledgedOrderResultDto> AcknowledgeStoppedOrderAsync(
        MedicalOrderEntity order, string nurseId)
    {
        _logger.LogInformation("签收停止医嘱，医嘱ID: {OrderId}", order.Id);

        // 更新签收护士
        order.NurseId = nurseId;
        await _orderRepository.UpdateAsync(order);

        // TODO: 阶段三实现 - 检查是否有已提交但未执行的申请
        // 查找该医嘱的所有待执行任务
        var pendingTasks = await _taskRepository.ListAsync(t =>
            t.MedicalOrderId == order.Id &&
            t.Status == "Pending");

        _logger.LogInformation("该停止医嘱有 {Count} 个待执行任务", pendingTasks.Count);

        var result = new AcknowledgedOrderResultDto
        {
            OrderId = order.Id,
            OrderType = order.OrderType,
            NeedTodayAction = false,
            ActionType = "None",
            GeneratedTaskIds = new List<long>(),
            // TODO: 阶段三实现 - 检查是否有待取消的药品申请
            HasPendingRequests = false,
            PendingRequestIds = new List<long>()
        };

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
                // 检查医嘱的任务生成发生在检查站创建预约时，签收阶段不生成任务
                _logger.LogInformation("检查医嘱已签收，任务将在检查站创建预约时生成");
                tasks = new List<ExecutionTask>();
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

        // 停止医嘱特有字段
        if (order.Status == "Stopped")
        {
            dto.StopTime = order.EndTime;
            // TODO: 需要在实体中添加StopReason字段
            dto.StopReason = "医生停止";
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
}
