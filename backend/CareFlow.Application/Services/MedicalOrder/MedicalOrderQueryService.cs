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
    private readonly IRepository<InspectionReport, long> _inspectionReportRepository;
    private readonly IRepository<OperationOrder, long> _operationRepository;
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IRepository<Doctor, string> _doctorRepository;
    private readonly IRepository<Nurse, string> _nurseRepository;
    private readonly IRepository<Drug, string> _drugRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly ILogger<MedicalOrderQueryService> _logger;

    public MedicalOrderQueryService(
        IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> orderRepository,
        IRepository<MedicationOrder, long> medicationRepository,
        IRepository<SurgicalOrder, long> surgicalRepository,
        IRepository<InspectionOrder, long> inspectionRepository,
        IRepository<InspectionReport, long> inspectionReportRepository,
        IRepository<OperationOrder, long> operationRepository,
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<Patient, string> patientRepository,
        IRepository<Doctor, string> doctorRepository,
        IRepository<Nurse, string> nurseRepository,
        IRepository<Drug, string> drugRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        ILogger<MedicalOrderQueryService> logger)
    {
        _orderRepository = orderRepository;
        _medicationRepository = medicationRepository;
        _surgicalRepository = surgicalRepository;
        _inspectionRepository = inspectionRepository;
        _inspectionReportRepository = inspectionReportRepository;
        _operationRepository = operationRepository;
        _taskRepository = taskRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _nurseRepository = nurseRepository;
        _drugRepository = drugRepository;
        _statusHistoryRepository = statusHistoryRepository;
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

            // 如果是检查医嘱，填充报告相关信息
            if (order.OrderType == "InspectionOrder")
            {
                var inspOrder = await _inspectionRepository.GetByIdAsync(order.Id);
                if (inspOrder != null)
                {
                    summary.ReportId = inspOrder.ReportId;
                    
                    // 如果有报告ID，获取报告的附件URL
                    if (!string.IsNullOrEmpty(inspOrder.ReportId) && long.TryParse(inspOrder.ReportId, out var reportId))
                    {
                        var report = await _inspectionReportRepository.GetByIdAsync(reportId);
                        if (report != null)
                        {
                            summary.AttachmentUrl = report.AttachmentUrl;
                        }
                    }
                }
            }

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
                .ThenInclude(p => p.Bed)
                    .ThenInclude(b => b.Ward)
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
            PatientGender = baseOrder.Patient?.Gender,
            PatientAge = baseOrder.Patient?.Age,
            BedNumber = baseOrder.Patient?.Bed?.Id,
            Department = baseOrder.Patient?.Bed?.Ward?.Id,
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

            // 2. 验证医嘱状态（PendingReceive、Accepted、InProgress 或 Stopped 可以停止）
            if (order.Status != OrderStatus.PendingReceive && 
                order.Status != OrderStatus.Accepted && 
                order.Status != OrderStatus.InProgress &&
                order.Status != OrderStatus.Stopped)
            {
                response.Success = false;
                response.Message = $"医嘱状态为 {order.Status}，不允许停止（仅 PendingReceive、Accepted、InProgress 或 Stopped 状态可停止）";
                response.Errors.Add($"当前状态: {order.Status}");
                return response;
            }

            // 2.1 特殊处理：未签收的医嘱直接取消，不需要护士签收
            if (order.Status == OrderStatus.PendingReceive)
            {
                _logger.LogInformation("检测到未签收医嘱，直接取消（不需要护士签收）");
                
                var oldStatus = order.Status;
                order.Status = OrderStatus.Cancelled;
                order.StopReason = request.StopReason;
                order.StopOrderTime = DateTime.UtcNow;
                order.StopDoctorId = request.DoctorId;
                
                await _orderRepository.UpdateAsync(order);
                
                // 记录状态历史
                var cancelHistory = new MedicalOrderStatusHistory
                {
                    MedicalOrderId = order.Id,
                    FromStatus = oldStatus,
                    ToStatus = OrderStatus.Cancelled,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = request.DoctorId,
                    ChangedByType = "Doctor",
                    Reason = $"医生取消未签收医嘱: {request.StopReason}"
                };
                await _statusHistoryRepository.AddAsync(cancelHistory);
                
                _logger.LogInformation("✅ 未签收医嘱已取消: {OrderId}", order.Id);
                
                response.Success = true;
                response.Message = "未签收医嘱已取消";
                response.OrderStatus = OrderStatus.Cancelled;
                response.StopOrderTime = order.StopOrderTime.Value;
                response.LockedTaskIds = new List<long>();
                response.LockedTasks = new List<LockedTaskDto>();
                
                return response;
            }

            // 3. 验证停止节点任务是否存在（已签收医嘱才需要验证）
            if (!request.StopAfterTaskId.HasValue || request.StopAfterTaskId.Value <= 0)
            {
                response.Success = false;
                response.Message = "已签收医嘱必须指定停止节点";
                response.Errors.Add("停止节点不能为空");
                return response;
            }
            
            var stopAfterTask = await _taskRepository.GetByIdAsync(request.StopAfterTaskId.Value);
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
            var stopAfterIndex = allTasks.FindIndex(t => t.Id == request.StopAfterTaskId.Value);
            if (stopAfterIndex == -1)
            {
                response.Success = false;
                response.Message = "无法定位停止节点";
                response.Errors.Add("停止节点定位失败");
                return response;
            }

            // 5.1 如果医嘱已经是Stopped状态，验证新的停止节点不能晚于之前的停止节点
            if (order.Status == OrderStatus.Stopped)
            {
                // 查找之前被停止的任务（状态为Stopped且有StatusBeforeLocking的）
                var previousStoppedTasks = allTasks
                    .Where(t => (t.Status == ExecutionTaskStatus.Stopped || t.Status == ExecutionTaskStatus.PendingReturn) && t.StatusBeforeLocking.HasValue)
                    .ToList();

                if (previousStoppedTasks.Any())
                {
                    // 找到之前停止的第一个任务的索引
                    var firstPreviousStoppedIndex = allTasks.FindIndex(t => 
                        t.Status == ExecutionTaskStatus.Stopped && t.StatusBeforeLocking.HasValue);

                    if (stopAfterIndex >= firstPreviousStoppedIndex)
                    {
                        response.Success = false;
                        response.Message = $"停止节点不能晚于或等于之前的停止节点（任务索引 {firstPreviousStoppedIndex}）";
                        response.Errors.Add("停止节点必须早于之前的停止节点");
                        _logger.LogWarning("停止节点 {NewIndex} 不能晚于之前的停止节点 {OldIndex}", 
                            stopAfterIndex, firstPreviousStoppedIndex);
                        return response;
                    }

                    _logger.LogInformation("✅ 已停止医嘱再次停止：新停止节点索引 {NewIndex}，之前停止节点索引 {OldIndex}",
                        stopAfterIndex, firstPreviousStoppedIndex);
                }
            }

            // 6. 筛选需要锁定的任务：停止节点及之后的所有待申请、已申请、就绪、待执行状态的任务
            var tasksToLock = allTasks
                .Skip(stopAfterIndex) // 包含停止节点及之后的任务
                .Where(t => t.Status == ExecutionTaskStatus.Applying 
                         || t.Status == ExecutionTaskStatus.Applied
                         || t.Status == ExecutionTaskStatus.AppliedConfirmed
                         || t.Status == ExecutionTaskStatus.Pending)
                .ToList();

            _logger.LogInformation("需要锁定 {LockCount} 个任务（包含停止节点，状态: 待申请/已申请/就绪/待执行）", tasksToLock.Count);

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

            // 8. 更新医嘱状态：InProgress/Accepted/Stopped → PendingStop
            var previousStatus = order.Status;
            
            order.Status = OrderStatus.PendingStop;
            order.StopReason = request.StopReason;
            order.StopOrderTime = DateTime.UtcNow;
            order.StopDoctorId = request.DoctorId;

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("✅ 医嘱状态从 {PreviousStatus} 更新为 PendingStop", previousStatus);

            // 插入状态历史记录
            var history = new MedicalOrderStatusHistory
            {
                MedicalOrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = OrderStatus.PendingStop,
                ChangedAt = DateTime.UtcNow,
                ChangedById = request.DoctorId,
                ChangedByType = "Doctor",
                Reason = $"医生下达停嘱指令: {request.StopReason}"
            };
            await _statusHistoryRepository.AddAsync(history);
            
            _logger.LogInformation("✅ 已记录状态变更历史: {FromStatus} → {ToStatus}",
                previousStatus, OrderStatus.PendingStop);

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
        detail.ItemName = inspOrder.ItemName;
        detail.RisLisId = inspOrder.RisLisId;
        detail.Location = inspOrder.Location;
        detail.Precautions = inspOrder.Precautions;
        detail.AppointmentTime = inspOrder.AppointmentTime;
        detail.AppointmentPlace = inspOrder.AppointmentPlace;
        detail.ReportId = inspOrder.ReportId;
        detail.ReportTime = inspOrder.ReportTime;
        
        // 如果有报告，获取报告附件URL
        if (!string.IsNullOrEmpty(inspOrder.ReportId) && long.TryParse(inspOrder.ReportId, out var reportId))
        {
            var report = await _inspectionReportRepository.GetByIdAsync(reportId);
            if (report != null)
            {
                detail.AttachmentUrl = report.AttachmentUrl;
            }
        }
    }

    /// <summary>
    /// 填充操作医嘱特定字段
    /// </summary>
    private async Task FillOperationOrderFieldsAsync(OrderDetailDto detail, long orderId)
    {
        var opOrder = await _operationRepository.GetByIdAsync(orderId);
        if (opOrder == null) return;

        // OperationOrder模型字段：OpId, OperationName, OperationSite, Normal, FrequencyType, FrequencyValue等
        detail.OperationCode = opOrder.OpId;
        detail.OperationName = opOrder.OperationName ?? opOrder.OpId; // 使用OperationName字段，如果没有则使用OpId
        detail.TargetSite = opOrder.OperationSite; // 操作部位
        
        // 操作类医嘱也有时间策略相关字段，需要填充
        detail.TimingStrategy = opOrder.TimingStrategy;
        detail.StartTime = opOrder.StartTime;
        detail.IntervalHours = opOrder.IntervalHours;
        detail.IntervalDays = opOrder.IntervalDays;
        detail.SmartSlotsMask = opOrder.SmartSlotsMask;
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
                    var drugName = drug?.GenericName ?? "药品";
                    
                    // 如果有多个药品，添加"等n种药品"
                    if (medOrder.Items.Count > 1)
                    {
                        return $"{drugName} {firstItem.Dosage} 等{medOrder.Items.Count}种药品";
                    }
                    return $"{drugName} {firstItem.Dosage}";
                }
                return "药品医嘱";

            case "SurgicalOrder":
                var surgOrder = await _surgicalRepository.GetByIdAsync(order.Id);
                return surgOrder?.SurgeryName ?? "手术医嘱";

            case "InspectionOrder":
                var inspOrder = await _inspectionRepository.GetByIdAsync(order.Id);
                return inspOrder?.ItemName ?? "检查医嘱";

            case "OperationOrder":
                var opOrder = await _operationRepository.GetByIdAsync(order.Id);
                return opOrder?.OperationName ?? opOrder?.OpId ?? "操作医嘱";

            default:
                return "医嘱";
        }
    }

    /// <summary>
    /// 重新提交已退回的医嘱
    /// 医嘱状态: Rejected → PendingReceive
    /// </summary>
    public async Task<bool> ResubmitRejectedOrderAsync(long orderId, string doctorId)
    {
        _logger.LogInformation("========== 医生重新提交已退回医嘱 ==========");
        _logger.LogInformation("医嘱ID: {OrderId}, 医生ID: {DoctorId}", orderId, doctorId);

        try
        {
            // 1. 查询医嘱
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("医嘱 {OrderId} 不存在", orderId);
                throw new Exception($"医嘱 {orderId} 不存在");
            }

            // 2. 验证状态
            if (order.Status != OrderStatus.Rejected)
            {
                _logger.LogWarning("医嘱 {OrderId} 状态为 {Status}，只能重新提交已退回的医嘱", orderId, order.Status);
                throw new Exception($"医嘱状态为 {order.Status}，只能重新提交已退回的医嘱");
            }

            // 3. 验证操作人
            if (order.DoctorId != doctorId)
            {
                _logger.LogWarning("医生 {DoctorId} 无权操作医嘱 {OrderId}（开单医生: {OriginalDoctorId}）",
                    doctorId, orderId, order.DoctorId);
                throw new Exception("只有开单医生才能重新提交医嘱");
            }

            var previousStatus = order.Status;

            // 4. 更新状态为 PendingReceive
            order.Status = OrderStatus.PendingReceive;
            order.ResubmittedAt = DateTime.UtcNow;
            
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("✅ 医嘱 {OrderId} 状态已从 Rejected 更新为 PendingReceive", orderId);

            // 5. 插入状态历史记录
            var history = new MedicalOrderStatusHistory
            {
                MedicalOrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = OrderStatus.PendingReceive,
                ChangedAt = DateTime.UtcNow,
                ChangedById = doctorId,
                ChangedByType = "Doctor",
                Reason = "医生重新提交已退回的医嘱"
            };
            await _statusHistoryRepository.AddAsync(history);

            _logger.LogInformation("✅ 已记录状态变更历史");
            _logger.LogInformation("========== 重新提交完成 ==========");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 重新提交医嘱失败: {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// 撤销已退回的医嘱
    /// 医嘱状态: Rejected → Cancelled
    /// </summary>
    public async Task<bool> CancelRejectedOrderAsync(long orderId, string doctorId, string cancelReason)
    {
        _logger.LogInformation("========== 医生撤销已退回医嘱 ==========");
        _logger.LogInformation("医嘱ID: {OrderId}, 医生ID: {DoctorId}, 原因: {Reason}",
            orderId, doctorId, cancelReason);

        try
        {
            // 1. 查询医嘱
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("医嘱 {OrderId} 不存在", orderId);
                throw new Exception($"医嘱 {orderId} 不存在");
            }

            // 2. 验证状态
            if (order.Status != OrderStatus.Rejected)
            {
                _logger.LogWarning("医嘱 {OrderId} 状态为 {Status}，只能撤销已退回的医嘱", orderId, order.Status);
                throw new Exception($"医嘱状态为 {order.Status}，只能撤销已退回的医嘱");
            }

            // 3. 验证操作人
            if (order.DoctorId != doctorId)
            {
                _logger.LogWarning("医生 {DoctorId} 无权操作医嘱 {OrderId}（开单医生: {OriginalDoctorId}）",
                    doctorId, orderId, order.DoctorId);
                throw new Exception("只有开单医生才能撤销医嘱");
            }

            var previousStatus = order.Status;

            // 4. 更新状态为 Cancelled
            order.Status = OrderStatus.Cancelled;
            order.CancelReason = cancelReason;
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledByDoctorId = doctorId;
            
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("✅ 医嘱 {OrderId} 状态已从 Rejected 更新为 Cancelled", orderId);

            // 5. 插入状态历史记录
            var history = new MedicalOrderStatusHistory
            {
                MedicalOrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = OrderStatus.Cancelled,
                ChangedAt = DateTime.UtcNow,
                ChangedById = doctorId,
                ChangedByType = "Doctor",
                Reason = $"医生撤销医嘱: {cancelReason}"
            };
            await _statusHistoryRepository.AddAsync(history);

            _logger.LogInformation("✅ 已记录状态变更历史");
            _logger.LogInformation("========== 撤销完成 ==========");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销医嘱失败: {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// 医生撤回停嘱申请
    /// 医嘱状态: PendingStop → 停止前的状态（Accepted/InProgress）
    /// 任务状态: OrderStopping → 锁定前的原始状态（StatusBeforeLocking）
    /// </summary>
    public async Task<WithdrawStopResponseDto> WithdrawStopAsync(WithdrawStopRequestDto request)
    {
        _logger.LogInformation("========== 医生撤回停嘱申请 ==========");
        _logger.LogInformation("医嘱ID: {OrderId}, 医生ID: {DoctorId}, 原因: {Reason}",
            request.OrderId, request.DoctorId, request.WithdrawReason);

        var response = new WithdrawStopResponseDto
        {
            OrderId = request.OrderId
        };

        try
        {
            // 1. 查询医嘱
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                response.Success = false;
                response.Message = $"医嘱 {request.OrderId} 不存在";
                response.Errors.Add("医嘱不存在");
                _logger.LogWarning("医嘱 {OrderId} 不存在", request.OrderId);
                return response;
            }

            // 2. 验证状态：必须是 PendingStop
            if (order.Status != OrderStatus.PendingStop)
            {
                response.Success = false;
                response.Message = $"医嘱状态为 {order.Status}，只能撤回等待停嘱（PendingStop）状态的医嘱";
                response.Errors.Add($"当前状态: {order.Status}");
                _logger.LogWarning("医嘱 {OrderId} 状态为 {Status}，不能撤回", request.OrderId, order.Status);
                return response;
            }

            var currentStatus = order.Status;

            // 3. 查询历史记录，获取停止前的状态
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
                // 默认恢复为 InProgress
                statusToRestore = OrderStatus.InProgress;
                _logger.LogWarning("未找到医嘱 {OrderId} 的停止前状态历史记录，默认恢复为 InProgress", request.OrderId);
            }

            // 4. 恢复医嘱状态
            order.Status = statusToRestore;
            
            // 清空停嘱相关字段（医生可能会再次下达停嘱）
            order.StopReason = null;
            order.StopOrderTime = null;
            order.StopDoctorId = null;
            order.StopConfirmedAt = null;
            order.StopConfirmedByNurseId = null;

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("✅ 医嘱 {OrderId} 状态已从 PendingStop 恢复为 {RestoredStatus}",
                request.OrderId, statusToRestore);

            // 5. 插入状态历史记录
            var history = new MedicalOrderStatusHistory
            {
                MedicalOrderId = order.Id,
                FromStatus = currentStatus,
                ToStatus = statusToRestore,
                ChangedAt = DateTime.UtcNow,
                ChangedById = request.DoctorId,
                ChangedByType = "Doctor",
                Reason = $"医生撤回停嘱申请: {request.WithdrawReason}"
            };
            await _statusHistoryRepository.AddAsync(history);

            // 6. 查找并恢复被锁定的任务
            var lockedTasks = await _taskRepository.ListAsync(t =>
                t.MedicalOrderId == order.Id &&
                t.Status == ExecutionTaskStatus.OrderStopping);

            _logger.LogInformation("医嘱 {OrderId} 有 {Count} 个被锁定的任务需要恢复",
                request.OrderId, lockedTasks.Count);

            var restoredTaskIds = new List<long>();
            var taskRestorationDetails = new Dictionary<long, string>();

            foreach (var task in lockedTasks)
            {
                // 恢复到锁定前的状态
                var restoredStatus = task.StatusBeforeLocking ?? ExecutionTaskStatus.Pending;
                var originalStatus = task.Status;

                task.Status = restoredStatus;
                task.StatusBeforeLocking = null; // 清空锁定前状态字段
                task.LastModifiedAt = DateTime.UtcNow;

                // 记录操作日志到 ExceptionReason（用于审计，转换为北京时间显示）
                var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var beijingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaTimeZone);
                var operationLog = $"[{beijingTime:yyyy-MM-dd HH:mm:ss}] 医生 {request.DoctorId} 撤回停嘱，" +
                                  $"任务从 {originalStatus} 恢复为 {restoredStatus}。" +
                                  $"原因: {request.WithdrawReason}";

                task.ExceptionReason = string.IsNullOrEmpty(task.ExceptionReason)
                    ? operationLog
                    : task.ExceptionReason + "\n" + operationLog;

                await _taskRepository.UpdateAsync(task);

                restoredTaskIds.Add(task.Id);
                taskRestorationDetails[task.Id] = restoredStatus.ToString();

                _logger.LogInformation("✅ 任务 {TaskId} 已从 OrderStopping 恢复为 {Status}",
                    task.Id, restoredStatus);
            }

            // 7. 返回成功响应
            response.Success = true;
            response.Message = $"撤回成功，医嘱恢复为 {statusToRestore}，{lockedTasks.Count} 个任务已解锁";
            response.RestoredOrderStatus = statusToRestore;
            response.RestoredTaskIds = restoredTaskIds;
            response.TaskRestorationDetails = taskRestorationDetails;

            _logger.LogInformation("✅ 医嘱 {OrderId} 停嘱已撤回，{TaskCount} 个任务已解锁",
                request.OrderId, lockedTasks.Count);
            _logger.LogInformation("========== 撤回停嘱完成 ==========");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤回停嘱失败: {OrderId}", request.OrderId);
            response.Success = false;
            response.Message = "撤回停嘱失败";
            response.Errors.Add($"系统错误: {ex.Message}");
            return response;
        }
    }
}
