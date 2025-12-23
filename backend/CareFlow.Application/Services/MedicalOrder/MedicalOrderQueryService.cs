using CareFlow.Application.DTOs.Common;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CareFlow.Application.Services.MedicalOrder;

/// <summary>
/// 医嘱查询服务实现
/// 提供医生端查询医嘱列表、查看医嘱详情、停止医嘱等功能
/// </summary>
public class MedicalOrderQueryService : IMedicalOrderQueryService
{
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> _orderRepository;
    private readonly IRepository<MedicationOrder, long> _medicationRepository;
    private readonly IRepository<SurgicalOrder, long> _surgicalRepository;
    private readonly IRepository<InspectionOrder, long> _inspectionRepository;
    private readonly IRepository<OperationOrder, long> _operationRepository;
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IRepository<Doctor, string> _doctorRepository;
    private readonly IRepository<Nurse, string> _nurseRepository;
    private readonly IRepository<Drug, string> _drugRepository;
    private readonly ILogger<MedicalOrderQueryService> _logger;

    public MedicalOrderQueryService(
        IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> orderRepository,
        IRepository<MedicationOrder, long> medicationRepository,
        IRepository<SurgicalOrder, long> surgicalRepository,
        IRepository<InspectionOrder, long> inspectionRepository,
        IRepository<OperationOrder, long> operationRepository,
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<Patient, string> patientRepository,
        IRepository<Doctor, string> doctorRepository,
        IRepository<Nurse, string> nurseRepository,
        IRepository<Drug, string> drugRepository,
        ILogger<MedicalOrderQueryService> logger)
    {
        _orderRepository = orderRepository;
        _medicationRepository = medicationRepository;
        _surgicalRepository = surgicalRepository;
        _inspectionRepository = inspectionRepository;
        _operationRepository = operationRepository;
        _taskRepository = taskRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _nurseRepository = nurseRepository;
        _drugRepository = drugRepository;
        _logger = logger;
    }

    /// <summary>
    /// 查询患者的医嘱列表（支持多条件筛选）
    /// </summary>
    public async Task<QueryOrdersResponseDto> GetOrdersByPatientAsync(QueryOrdersRequestDto request)
    {
        _logger.LogInformation("查询患者 {PatientId} 的医嘱列表", request.PatientId);

        // 基础查询：按患者筛选
        var query = _orderRepository.GetQueryable()
            .Include(o => o.Doctor)
            .Include(o => o.Nurse)
            .Where(o => o.PatientId == request.PatientId);

        // 状态筛选
        if (request.Statuses != null && request.Statuses.Any())
        {
            query = query.Where(o => request.Statuses.Contains(o.Status));
        }

        // 类型筛选
        if (request.OrderTypes != null && request.OrderTypes.Any())
        {
            query = query.Where(o => request.OrderTypes.Contains(o.OrderType));
        }

        // 时间范围筛选
        if (request.CreateTimeFrom.HasValue)
        {
            query = query.Where(o => o.CreateTime >= request.CreateTimeFrom.Value);
        }

        if (request.CreateTimeTo.HasValue)
        {
            query = query.Where(o => o.CreateTime <= request.CreateTimeTo.Value);
        }

        // 排序
        query = request.SortBy?.ToLower() switch
        {
            "status" => request.SortDescending 
                ? query.OrderByDescending(o => o.Status) 
                : query.OrderBy(o => o.Status),
            "ordertype" => request.SortDescending 
                ? query.OrderByDescending(o => o.OrderType) 
                : query.OrderBy(o => o.OrderType),
            _ => request.SortDescending 
                ? query.OrderByDescending(o => o.CreateTime) 
                : query.OrderBy(o => o.CreateTime)
        };

        // 执行查询
        var orders = await query.ToListAsync();
        var totalCount = orders.Count;

        _logger.LogInformation("找到 {Count} 条医嘱", totalCount);

        // 获取患者信息
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);

        // 映射到 DTO
        var orderSummaries = new List<OrderSummaryDto>();

        foreach (var order in orders)
        {
            // 获取任务统计
            var tasks = await _taskRepository.GetQueryable()
                .Where(t => t.MedicalOrderId == order.Id)
                .ToListAsync();

            var summary = new OrderSummaryDto
            {
                Id = order.Id,
                OrderType = order.OrderType,
                Status = order.Status,
                IsLongTerm = order.IsLongTerm,
                CreateTime = order.CreateTime,
                PlantEndTime = order.PlantEndTime,
                DoctorId = order.DoctorId,
                DoctorName = order.Doctor?.Name ?? "未知医生",
                TaskCount = tasks.Count,
                CompletedTaskCount = tasks.Count(t => t.Status == ExecutionTaskStatus.Completed),
                StopOrderTime = order.StopOrderTime,
                StopReason = order.StopReason,
                Summary = await GenerateOrderSummaryAsync(order)
            };

            orderSummaries.Add(summary);
        }

        return new QueryOrdersResponseDto
        {
            Orders = orderSummaries,
            TotalCount = totalCount,
            PatientId = request.PatientId,
            PatientName = patient?.Name ?? "未知患者"
        };
    }

    /// <summary>
    /// 获取单个医嘱的详细信息（包含关联的执行任务列表）
    /// </summary>
    public async Task<OrderDetailDto> GetOrderDetailAsync(long orderId)
    {
        _logger.LogInformation("获取医嘱 {OrderId} 的详细信息", orderId);

        // 获取基础医嘱
        var baseOrder = await _orderRepository.GetQueryable()
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Include(o => o.Nurse)
            .Include(o => o.SignedByNurse)
            .Include(o => o.StopDoctor)
            .Include(o => o.StopConfirmedByNurse)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (baseOrder == null)
        {
            throw new Exception($"医嘱 {orderId} 不存在");
        }

        // 创建基础 DTO
        var detail = new OrderDetailDto
        {
            Id = baseOrder.Id,
            OrderType = baseOrder.OrderType,
            Status = baseOrder.Status,
            IsLongTerm = baseOrder.IsLongTerm,
            CreateTime = baseOrder.CreateTime,
            PlantEndTime = baseOrder.PlantEndTime,
            EndTime = baseOrder.EndTime,
            Remarks = baseOrder.Remarks,
            PatientId = baseOrder.PatientId,
            PatientName = baseOrder.Patient?.Name ?? "未知患者",
            DoctorId = baseOrder.DoctorId,
            DoctorName = baseOrder.Doctor?.Name ?? "未知医生",
            NurseId = baseOrder.NurseId,
            NurseName = baseOrder.Nurse?.Name,
            SignedByNurseId = baseOrder.SignedByNurseId,
            SignedByNurseName = baseOrder.SignedByNurse?.Name,
            SignedAt = baseOrder.SignedAt,
            StopReason = baseOrder.StopReason,
            StopOrderTime = baseOrder.StopOrderTime,
            StopDoctorId = baseOrder.StopDoctorId,
            StopDoctorName = baseOrder.StopDoctor?.Name,
            StopConfirmedAt = baseOrder.StopConfirmedAt,
            StopConfirmedByNurseId = baseOrder.StopConfirmedByNurseId,
            StopConfirmedByNurseName = baseOrder.StopConfirmedByNurse?.Name
        };

        // 根据类型填充特定字段
        await FillOrderTypeSpecificFieldsAsync(detail, orderId, baseOrder.OrderType);

        // 获取关联任务
        detail.Tasks = await GetOrderTasksAsync(orderId);

        _logger.LogInformation("医嘱详情获取成功，包含 {TaskCount} 个任务", detail.Tasks.Count);

        return detail;
    }

    /// <summary>
    /// 医生停止医嘱（核心功能）
    /// </summary>
    public async Task<StopOrderResponseDto> StopOrderAsync(StopOrderRequestDto request)
    {
        _logger.LogInformation("========== 医生停止医嘱 ==========");
        _logger.LogInformation("医嘱ID: {OrderId}, 医生ID: {DoctorId}, 停止节点: {StopAfterTaskId}", 
            request.OrderId, request.DoctorId, request.StopAfterTaskId);

        var response = new StopOrderResponseDto
        {
            OrderId = request.OrderId
        };

        try
        {
            // 1. 获取医嘱
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                response.Success = false;
                response.Message = $"医嘱 {request.OrderId} 不存在";
                response.Errors.Add("医嘱不存在");
                return response;
            }

            // 2. 验证医嘱状态（只有 Accepted 或 InProgress 可以停止）
            if (order.Status != OrderStatus.Accepted && order.Status != OrderStatus.InProgress)
            {
                response.Success = false;
                response.Message = $"医嘱状态为 {order.Status}，不允许停止（仅 Accepted 或 InProgress 状态可停止）";
                response.Errors.Add($"当前状态: {order.Status}");
                return response;
            }

            // 3. 验证停止节点任务是否存在
            var stopAfterTask = await _taskRepository.GetByIdAsync(request.StopAfterTaskId);
            if (stopAfterTask == null || stopAfterTask.MedicalOrderId != request.OrderId)
            {
                response.Success = false;
                response.Message = "停止节点任务不存在或不属于该医嘱";
                response.Errors.Add("停止节点无效");
                return response;
            }

            // 4. 获取该医嘱的所有任务（按计划时间排序）
            var allTasks = await _taskRepository.GetQueryable()
                .Where(t => t.MedicalOrderId == request.OrderId)
                .OrderBy(t => t.PlannedStartTime)
                .ToListAsync();

            _logger.LogInformation("医嘱共有 {TotalTasks} 个任务", allTasks.Count);

            // 5. 找到停止节点在任务列表中的位置
            var stopAfterIndex = allTasks.FindIndex(t => t.Id == request.StopAfterTaskId);
            if (stopAfterIndex == -1)
            {
                response.Success = false;
                response.Message = "无法定位停止节点";
                response.Errors.Add("停止节点定位失败");
                return response;
            }

            // 6. 筛选需要锁定的任务：停止节点之后的所有未完成任务
            var tasksToLock = allTasks
                .Skip(stopAfterIndex + 1) // 跳过停止节点及之前的任务
                .Where(t => t.Status != ExecutionTaskStatus.Completed 
                         && t.Status != ExecutionTaskStatus.Stopped)
                .ToList();

            _logger.LogInformation("需要锁定 {LockCount} 个任务", tasksToLock.Count);

            // 7. 锁定任务：保存原状态 → 改为 OrderStopping
            var lockedTasks = new List<LockedTaskDto>();

            foreach (var task in tasksToLock)
            {
                var originalStatus = task.Status;
                
                // 保存锁定前状态
                task.StatusBeforeLocking = originalStatus;
                
                // 改为锁定状态
                task.Status = ExecutionTaskStatus.OrderStopping;
                task.LastModifiedAt = DateTime.UtcNow;

                await _taskRepository.UpdateAsync(task);

                lockedTasks.Add(new LockedTaskDto
                {
                    TaskId = task.Id,
                    PlannedStartTime = task.PlannedStartTime,
                    StatusBeforeLocking = originalStatus,
                    CurrentStatus = ExecutionTaskStatus.OrderStopping
                });

                _logger.LogInformation("✅ 任务 {TaskId} 已锁定: {OldStatus} → OrderStopping", 
                    task.Id, originalStatus);
            }

            // 8. 更新医嘱状态：InProgress/Accepted → PendingStop
            order.Status = OrderStatus.PendingStop;
            order.StopReason = request.StopReason;
            order.StopOrderTime = DateTime.UtcNow;
            order.StopDoctorId = request.DoctorId;

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("✅ 医嘱状态更新为 PendingStop");

            // 9. 返回成功响应
            response.Success = true;
            response.Message = $"停嘱成功，已锁定 {tasksToLock.Count} 个任务，等待护士签收";
            response.OrderStatus = OrderStatus.PendingStop;
            response.StopOrderTime = order.StopOrderTime.Value;
            response.LockedTaskIds = tasksToLock.Select(t => t.Id).ToList();
            response.LockedTasks = lockedTasks;

            _logger.LogInformation("========== 停嘱操作完成 ==========");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 停止医嘱时发生异常");
            response.Success = false;
            response.Message = "停嘱失败";
            response.Errors.Add($"系统错误: {ex.Message}");
            return response;
        }
    }

    // ==================== 私有辅助方法 ====================

    /// <summary>
    /// 根据医嘱类型填充特定字段
    /// </summary>
    private async Task FillOrderTypeSpecificFieldsAsync(OrderDetailDto detail, long orderId, string orderType)
    {
        switch (orderType)
        {
            case "MedicationOrder":
                await FillMedicationOrderFieldsAsync(detail, orderId);
                break;
            case "SurgicalOrder":
                await FillSurgicalOrderFieldsAsync(detail, orderId);
                break;
            case "InspectionOrder":
                await FillInspectionOrderFieldsAsync(detail, orderId);
                break;
            case "OperationOrder":
                await FillOperationOrderFieldsAsync(detail, orderId);
                break;
        }
    }

    /// <summary>
    /// 填充药品医嘱特定字段
    /// </summary>
    private async Task FillMedicationOrderFieldsAsync(OrderDetailDto detail, long orderId)
    {
        var medOrder = await _medicationRepository.GetQueryable()
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.Id == orderId);

        if (medOrder == null) return;

        detail.UsageRoute = medOrder.UsageRoute;
        detail.TimingStrategy = medOrder.TimingStrategy;
        detail.StartTime = medOrder.StartTime;
        detail.IntervalHours = medOrder.IntervalHours;
        detail.IntervalDays = medOrder.IntervalDays;
        detail.SmartSlotsMask = medOrder.SmartSlotsMask;

        // 填充药品项目列表
        detail.MedicationItems = new List<MedicationItemDto>();
        foreach (var item in medOrder.Items ?? new List<MedicationOrderItem>())
        {
            var drug = await _drugRepository.GetByIdAsync(item.DrugId);
            detail.MedicationItems.Add(new MedicationItemDto
            {
                Id = item.Id,
                DrugId = item.DrugId,
                DrugName = drug?.GenericName ?? "未知药品",
                Dosage = item.Dosage,
                Note = item.Note
            });
        }
    }

    /// <summary>
    /// 填充手术医嘱特定字段
    /// </summary>
    private async Task FillSurgicalOrderFieldsAsync(OrderDetailDto detail, long orderId)
    {
        var surgOrder = await _surgicalRepository.GetQueryable()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == orderId);

        if (surgOrder == null) return;

        detail.SurgeryName = surgOrder.SurgeryName;
        detail.ScheduleTime = surgOrder.ScheduleTime;
        detail.AnesthesiaType = surgOrder.AnesthesiaType;
        detail.IncisionSite = surgOrder.IncisionSite;
        detail.SurgeonId = surgOrder.SurgeonId;
        
        // 获取主刀医生姓名
        var surgeon = await _doctorRepository.GetByIdAsync(surgOrder.SurgeonId);
        detail.SurgeonName = surgeon?.Name ?? "未知医生";
        
        // JSON 反序列化术前宣讲和操作
        try
        {
            detail.RequiredTalk = string.IsNullOrEmpty(surgOrder.RequiredTalk) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(surgOrder.RequiredTalk) ?? new List<string>();
            
            detail.RequiredOperation = string.IsNullOrEmpty(surgOrder.RequiredOperation) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(surgOrder.RequiredOperation) ?? new List<string>();
        }
        catch (JsonException)
        {
            detail.RequiredTalk = new List<string>();
            detail.RequiredOperation = new List<string>();
        }

        // 填充手术药品项目
        detail.SurgicalItems = new List<MedicationItemDto>();
        foreach (var item in surgOrder.Items ?? new List<MedicationOrderItem>())
        {
            var drug = await _drugRepository.GetByIdAsync(item.DrugId);
            detail.SurgicalItems.Add(new MedicationItemDto
            {
                Id = item.Id,
                DrugId = item.DrugId,
                DrugName = drug?.GenericName ?? "未知药品",
                Dosage = item.Dosage,
                Note = item.Note
            });
        }
    }

    /// <summary>
    /// 填充检查医嘱特定字段
    /// </summary>
    private async Task FillInspectionOrderFieldsAsync(OrderDetailDto detail, long orderId)
    {
        var inspOrder = await _inspectionRepository.GetByIdAsync(orderId);
        if (inspOrder == null) return;

        detail.ItemCode = inspOrder.ItemCode;
        // InspectionOrder模型中没有ItemName字段，使用ItemCode作为名称
        detail.ItemName = inspOrder.ItemCode;
    }

    /// <summary>
    /// 填充操作医嘱特定字段
    /// </summary>
    private async Task FillOperationOrderFieldsAsync(OrderDetailDto detail, long orderId)
    {
        var opOrder = await _operationRepository.GetByIdAsync(orderId);
        if (opOrder == null) return;

        // OperationOrder模型字段：OpId, Normal, FrequencyType, FrequencyValue
        detail.OperationCode = opOrder.OpId;
        detail.OperationName = opOrder.OpId; // 模型中没有单独的名称字段
        detail.TargetSite = null; // 模型中没有此字段
    }

    /// <summary>
    /// 获取医嘱关联的任务列表
    /// </summary>
    private async Task<List<TaskSummaryDto>> GetOrderTasksAsync(long orderId)
    {
        var tasks = await _taskRepository.GetQueryable()
            .Include(t => t.Executor)
            .Where(t => t.MedicalOrderId == orderId)
            .OrderBy(t => t.PlannedStartTime)
            .ToListAsync();

        return tasks.Select(t => new TaskSummaryDto
        {
            Id = t.Id,
            Status = t.Status,
            PlannedStartTime = t.PlannedStartTime,
            ActualStartTime = t.ActualStartTime,
            ActualEndTime = t.ActualEndTime,
            Category = t.Category,
            ExecutorStaffId = t.ExecutorStaffId,
            ExecutorName = t.Executor?.Name,
            StatusBeforeLocking = t.StatusBeforeLocking,
            ExceptionReason = t.ExceptionReason
        }).ToList();
    }

    /// <summary>
    /// 生成医嘱摘要描述
    /// </summary>
    private async Task<string> GenerateOrderSummaryAsync(CareFlow.Core.Models.Medical.MedicalOrder order)
    {
        switch (order.OrderType)
        {
            case "MedicationOrder":
                var medOrder = await _medicationRepository.GetQueryable()
                    .Include(m => m.Items)
                    .FirstOrDefaultAsync(m => m.Id == order.Id);
                if (medOrder?.Items?.Any() == true)
                {
                    var firstItem = medOrder.Items.First();
                    var drug = await _drugRepository.GetByIdAsync(firstItem.DrugId);
                    return $"{drug?.GenericName ?? "药品"} {firstItem.Dosage}";
                }
                return "药品医嘱";

            case "SurgicalOrder":
                var surgOrder = await _surgicalRepository.GetByIdAsync(order.Id);
                return surgOrder?.SurgeryName ?? "手术医嘱";

            case "InspectionOrder":
                var inspOrder = await _inspectionRepository.GetByIdAsync(order.Id);
                return inspOrder?.ItemCode ?? "检查医嘱";

            case "OperationOrder":
                var opOrder = await _operationRepository.GetByIdAsync(order.Id);
                return opOrder?.OpId ?? "操作医嘱";

            default:
                return "医嘱";
        }
    }
}
