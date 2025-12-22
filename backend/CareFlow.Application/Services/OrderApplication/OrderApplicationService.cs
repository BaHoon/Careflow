using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CareFlow.Application.Services.OrderApplication;

/// <summary>
/// 医嘱申请服务实现
/// </summary>
public class OrderApplicationService : IOrderApplicationService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<InspectionOrder, long> _inspectionOrderRepository;
    private readonly IRepository<MedicationOrder, long> _medicationOrderRepository;
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IPharmacyIntegrationService _pharmacyService;
    private readonly IInspectionStationService _inspectionStationService;
    private readonly ILogger<OrderApplicationService> _logger;

    public OrderApplicationService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<InspectionOrder, long> inspectionOrderRepository,
        IRepository<MedicationOrder, long> medicationOrderRepository,
        IRepository<Patient, string> patientRepository,
        IPharmacyIntegrationService pharmacyService,
        IInspectionStationService inspectionStationService,
        ILogger<OrderApplicationService> logger)
    {
        _taskRepository = taskRepository;
        _inspectionOrderRepository = inspectionOrderRepository;
        _medicationOrderRepository = medicationOrderRepository;
        _patientRepository = patientRepository;
        _pharmacyService = pharmacyService;
        _inspectionStationService = inspectionStationService;
        _logger = logger;
    }

    #region 查询方法

    /// <summary>
    /// 获取药品申请列表
    /// </summary>
    public async Task<List<ApplicationItemDto>> GetMedicationApplicationsAsync(
        GetApplicationListRequestDto request)
    {
        _logger.LogInformation("========== 获取药品申请列表 ==========");
        _logger.LogInformation("患者数量: {Count}, 状态筛选: {Status}", 
            request.PatientIds.Count, 
            string.Join(",", request.StatusFilter ?? new List<string> { "全部" }));

        try
        {
            // 构建查询
            var query = _taskRepository.GetQueryable()
                .Include(t => t.Patient)
                    .ThenInclude(p => p.Bed)
                .Include(t => t.MedicalOrder)
                    .ThenInclude(o => ((MedicationOrder)o).Items)
                        .ThenInclude(item => item.Drug)
                .Where(t => request.PatientIds.Contains(t.PatientId) 
                         && t.Category == TaskCategory.Verification); // 取药任务

            // 状态筛选
            if (request.StatusFilter != null && request.StatusFilter.Any())
            {
                var statusEnums = request.StatusFilter
                    .Select(s => Enum.Parse<ExecutionTaskStatus>(s))
                    .ToList();
                query = query.Where(t => statusEnums.Contains(t.Status));
            }
            else
            {
                // 默认只查询待申请、已申请、已确认的任务
                query = query.Where(t => t.Status == ExecutionTaskStatus.Applying 
                                      || t.Status == ExecutionTaskStatus.Applied 
                                      || t.Status == ExecutionTaskStatus.AppliedConfirmed);
            }

            // 时间范围筛选
            if (request.StartTime.HasValue)
            {
                query = query.Where(t => t.PlannedStartTime >= request.StartTime.Value);
            }
            if (request.EndTime.HasValue)
            {
                query = query.Where(t => t.PlannedStartTime <= request.EndTime.Value);
            }

            // 执行查询
            var tasks = await query
                .OrderBy(t => t.PlannedStartTime)
                .ToListAsync();

            _logger.LogInformation("查询到 {Count} 条取药任务", tasks.Count);

            // 转换为DTO
            var result = new List<ApplicationItemDto>();
            foreach (var task in tasks)
            {
                try
                {
                    var dto = await MapTaskToApplicationItemDto(task);
                    result.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "映射任务 {TaskId} 失败", task.Id);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取药品申请列表失败");
            throw;
        }
    }

    /// <summary>
    /// 获取检查申请列表
    /// </summary>
    public async Task<List<ApplicationItemDto>> GetInspectionApplicationsAsync(
        GetApplicationListRequestDto request)
    {
        _logger.LogInformation("========== 获取检查申请列表 ==========");
        _logger.LogInformation("患者数量: {Count}", request.PatientIds.Count);

        try
        {
            // 查询已签收但未申请的检查医嘱
            var query = _inspectionOrderRepository.GetQueryable()
                .Include(o => o.Patient)
                    .ThenInclude(p => p.Bed)
                .Where(o => request.PatientIds.Contains(o.PatientId)
                         && o.Status == OrderStatus.Accepted  // 已签收
                         && o.InspectionStatus == InspectionOrderStatus.Pending); // 待前往

            var orders = await query
                .OrderBy(o => o.CreateTime)
                .ToListAsync();

            _logger.LogInformation("查询到 {Count} 条待申请检查医嘱", orders.Count);

            // 转换为DTO
            var result = new List<ApplicationItemDto>();
            foreach (var order in orders)
            {
                try
                {
                    var dto = await MapInspectionOrderToApplicationItemDto(order);
                    result.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "映射检查医嘱 {OrderId} 失败", order.Id);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取检查申请列表失败");
            throw;
        }
    }

    #endregion

    #region 提交申请方法

    /// <summary>
    /// 提交药品申请
    /// </summary>
    public async Task<ApplicationResponseDto> SubmitMedicationApplicationAsync(
        MedicationApplicationRequestDto request)
    {
        _logger.LogInformation("========== 提交药品申请 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 任务数: {Count}, 加急: {IsUrgent}",
            request.NurseId, request.TaskIds.Count, request.IsUrgent);

        var processedIds = new List<long>();
        var errors = new List<string>();

        try
        {
            // 1. 验证所有任务
            foreach (var taskId in request.TaskIds)
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    errors.Add($"任务 {taskId} 不存在");
                    continue;
                }

                if (task.Status != ExecutionTaskStatus.Applying)
                {
                    errors.Add($"任务 {taskId} 状态为 {task.Status}，不能申请");
                    continue;
                }

                // 2. 更新任务状态
                task.Status = ExecutionTaskStatus.Applied;
                task.LastModifiedAt = DateTime.UtcNow;

                // 3. 更新DataPayload，添加申请信息
                try
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        task.DataPayload);
                    
                    if (payload != null)
                    {
                        payload["ApplicationInfo"] = JsonSerializer.SerializeToElement(new
                        {
                            IsUrgent = request.IsUrgent,
                            AppliedAt = DateTime.UtcNow,
                            AppliedBy = request.NurseId,
                            Remarks = request.Remarks ?? ""
                        });

                        task.DataPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                    }
                }
                catch (Exception payloadEx)
                {
                    _logger.LogWarning(payloadEx, "更新DataPayload失败，任务ID: {TaskId}", taskId);
                }

                await _taskRepository.UpdateAsync(task);
                processedIds.Add(taskId);
                _logger.LogInformation("✅ 任务 {TaskId} 状态已更新为Applied", taskId);
            }

            if (processedIds.Count == 0)
            {
                return new ApplicationResponseDto
                {
                    Success = false,
                    Message = "所有任务申请失败",
                    Errors = errors
                };
            }

            // 4. 调用药房系统接口
            var pharmacyResult = await _pharmacyService.SendMedicationRequestAsync(
                processedIds, request.IsUrgent);

            if (!pharmacyResult.Success)
            {
                _logger.LogWarning("⚠️ 药房系统接口调用失败: {Message}", pharmacyResult.Message);
            }

            return new ApplicationResponseDto
            {
                Success = true,
                Message = errors.Count > 0
                    ? $"成功申请 {processedIds.Count} 个任务，失败 {errors.Count} 个"
                    : $"成功申请 {processedIds.Count} 个任务",
                ProcessedIds = processedIds,
                Errors = errors.Count > 0 ? errors : null,
                EstimatedCompletionTime = pharmacyResult.EstimatedCompletionTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 提交药品申请失败");
            throw;
        }
    }

    /// <summary>
    /// 提交检查申请
    /// </summary>
    public async Task<ApplicationResponseDto> SubmitInspectionApplicationAsync(
        InspectionApplicationRequestDto request)
    {
        _logger.LogInformation("========== 提交检查申请 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 医嘱数: {Count}, 加急: {IsUrgent}",
            request.NurseId, request.OrderIds.Count, request.IsUrgent);

        var processedIds = new List<long>();
        var errors = new List<string>();

        try
        {
            // 1. 验证所有检查医嘱
            foreach (var orderId in request.OrderIds)
            {
                var order = await _inspectionOrderRepository.GetByIdAsync(orderId);
                
                if (order == null)
                {
                    errors.Add($"检查医嘱 {orderId} 不存在");
                    continue;
                }

                if (order.Status != OrderStatus.Accepted)
                {
                    errors.Add($"检查医嘱 {orderId} 状态为 {order.Status}，不能申请");
                    continue;
                }

                if (order.InspectionStatus != InspectionOrderStatus.Pending)
                {
                    errors.Add($"检查医嘱 {orderId} 检查状态为 {order.InspectionStatus}，不能重复申请");
                    continue;
                }

                // 2. 更新检查医嘱状态（标记为已申请）
                // 注意：这里不修改InspectionStatus，等待检查站确认后才更新
                order.Remarks = string.IsNullOrEmpty(order.Remarks)
                    ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] 护士{request.NurseId}提交申请"
                    : order.Remarks + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] 护士{request.NurseId}提交申请";

                if (request.IsUrgent)
                {
                    order.Remarks += " (加急)";
                }

                await _inspectionOrderRepository.UpdateAsync(order);
                processedIds.Add(orderId);
                _logger.LogInformation("✅ 检查医嘱 {OrderId} 已标记为申请", orderId);
            }

            if (processedIds.Count == 0)
            {
                return new ApplicationResponseDto
                {
                    Success = false,
                    Message = "所有检查医嘱申请失败",
                    Errors = errors
                };
            }

            // 3. 调用检查站系统接口
            var inspectionResult = await _inspectionStationService.SendInspectionRequestAsync(
                processedIds, request.IsUrgent);

            if (!inspectionResult.Success)
            {
                _logger.LogWarning("⚠️ 检查站系统接口调用失败: {Message}", inspectionResult.Message);
            }

            return new ApplicationResponseDto
            {
                Success = true,
                Message = errors.Count > 0
                    ? $"成功申请 {processedIds.Count} 个检查，失败 {errors.Count} 个"
                    : $"成功申请 {processedIds.Count} 个检查",
                ProcessedIds = processedIds,
                Errors = errors.Count > 0 ? errors : null,
                AppointmentInfo = inspectionResult.AppointmentNumbers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 提交检查申请失败");
            throw;
        }
    }

    #endregion

    #region 撤销申请方法

    /// <summary>
    /// 撤销药品申请
    /// </summary>
    public async Task<ApplicationResponseDto> CancelMedicationApplicationAsync(
        List<long> taskIds, string nurseId, string? reason = null)
    {
        _logger.LogInformation("========== 撤销药品申请 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 任务数: {Count}", nurseId, taskIds.Count);

        var processedIds = new List<long>();
        var errors = new List<string>();

        try
        {
            foreach (var taskId in taskIds)
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    errors.Add($"任务 {taskId} 不存在");
                    continue;
                }

                // 只有Applied和AppliedConfirmed状态的任务可以撤销
                // InProgress（执行中）和结束状态不能撤销
                var canCancel = task.Status == ExecutionTaskStatus.Applied || 
                               task.Status == ExecutionTaskStatus.AppliedConfirmed;
                
                if (!canCancel)
                {
                    var reason_msg = task.Status switch
                    {
                        ExecutionTaskStatus.InProgress => "任务正在执行中",
                        ExecutionTaskStatus.Completed => "任务已完成",
                        ExecutionTaskStatus.OrderStopping => "停嘱锁定",
                        ExecutionTaskStatus.Stopped => "任务已停止/作废",
                        ExecutionTaskStatus.Incomplete => "任务异常/拒绝",
                        _ => "当前状态不允许撤销"
                    };
                    errors.Add($"任务 {taskId} {reason_msg}（{task.Status}），不能撤销");
                    continue;
                }

                // 更新状态回到Applying
                task.Status = ExecutionTaskStatus.Applying;
                task.LastModifiedAt = DateTime.UtcNow;
                task.ExceptionReason = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] 护士{nurseId}撤销申请: {reason ?? "无"}";

                await _taskRepository.UpdateAsync(task);
                processedIds.Add(taskId);
                _logger.LogInformation("✅ 任务 {TaskId} 已撤销", taskId);
            }

            // 调用药房系统撤销接口
            if (processedIds.Count > 0)
            {
                await _pharmacyService.CancelMedicationRequestAsync(processedIds);
            }

            return new ApplicationResponseDto
            {
                Success = processedIds.Count > 0,
                Message = errors.Count > 0
                    ? $"成功撤销 {processedIds.Count} 个任务，失败 {errors.Count} 个"
                    : $"成功撤销 {processedIds.Count} 个任务",
                ProcessedIds = processedIds,
                Errors = errors.Count > 0 ? errors : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销药品申请失败");
            throw;
        }
    }

    /// <summary>
    /// 撤销检查申请
    /// </summary>
    public async Task<ApplicationResponseDto> CancelInspectionApplicationAsync(
        List<long> orderIds, string nurseId, string? reason = null)
    {
        // TODO：可撤销逻辑
        _logger.LogInformation("========== 撤销检查申请 ==========");
        _logger.LogInformation("护士ID: {NurseId}, 医嘱数: {Count}", nurseId, orderIds.Count);

        var processedIds = new List<long>();
        var errors = new List<string>();

        try
        {
            foreach (var orderId in orderIds)
            {
                var order = await _inspectionOrderRepository.GetByIdAsync(orderId);
                
                if (order == null)
                {
                    errors.Add($"检查医嘱 {orderId} 不存在");
                    continue;
                }

                // 记录撤销信息
                order.Remarks += $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] 护士{nurseId}撤销申请: {reason ?? "无"}";

                await _inspectionOrderRepository.UpdateAsync(order);
                processedIds.Add(orderId);
                _logger.LogInformation("✅ 检查医嘱 {OrderId} 申请已撤销", orderId);
            }

            // 调用检查站系统撤销接口
            if (processedIds.Count > 0)
            {
                await _inspectionStationService.CancelInspectionRequestAsync(processedIds);
            }

            return new ApplicationResponseDto
            {
                Success = processedIds.Count > 0,
                Message = errors.Count > 0
                    ? $"成功撤销 {processedIds.Count} 个检查，失败 {errors.Count} 个"
                    : $"成功撤销 {processedIds.Count} 个检查",
                ProcessedIds = processedIds,
                Errors = errors.Count > 0 ? errors : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销检查申请失败");
            throw;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 将ExecutionTask映射为ApplicationItemDto
    /// </summary>
    private async Task<ApplicationItemDto> MapTaskToApplicationItemDto(ExecutionTask task)
    {
        // 解析DataPayload获取申请信息
        bool isUrgent = false;
        string? remarks = null;
        DateTime? appliedAt = null;
        string? appliedBy = null;
        DateTime? confirmedAt = null;

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                task.DataPayload);
            
            if (payload != null && payload.ContainsKey("ApplicationInfo"))
            {
                var appInfo = payload["ApplicationInfo"];
                isUrgent = appInfo.TryGetProperty("IsUrgent", out var urgent) && urgent.GetBoolean();
                remarks = appInfo.TryGetProperty("Remarks", out var rem) ? rem.GetString() : null;
                appliedAt = appInfo.TryGetProperty("AppliedAt", out var applAt) 
                    ? applAt.GetDateTime() : null;
                appliedBy = appInfo.TryGetProperty("AppliedBy", out var applBy) 
                    ? applBy.GetString() : null;
            }

            if (payload != null && payload.ContainsKey("PharmacyConfirmedAt"))
            {
                confirmedAt = payload["PharmacyConfirmedAt"].GetDateTime();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析DataPayload失败，任务ID: {TaskId}", task.Id);
        }

        // 获取药品信息
        var medications = new List<MedicationItemDetail>();
        if (task.MedicalOrder is MedicationOrder medOrder && medOrder.Items != null)
        {
            foreach (var item in medOrder.Items)
            {
                medications.Add(new MedicationItemDetail
                {
                    DrugId = item.DrugId,
                    DrugName = item.Drug?.GenericName ?? item.Drug?.TradeName ?? "未知药品",
                    Specification = item.Drug?.Specification ?? "",
                    Dosage = item.Dosage
                });
            }
        }

        var contentDesc = medications.Any()
            ? $"取药：{string.Join("、", medications.Select(m => m.DrugName))}"
            : "取药任务";

        // 构建显示文本：多药品时显示第一个 + "等"
        string displayText;
        if (medications.Count > 1)
        {
            displayText = $"{medications[0].DrugName}等";
        }
        else if (medications.Count == 1)
        {
            displayText = medications[0].DrugName;
        }
        else
        {
            displayText = "取药任务";
        }

        return new ApplicationItemDto
        {
            ApplicationType = "Medication",
            RelatedId = task.Id,
            OrderId = task.MedicalOrderId,
            OrderType = task.MedicalOrder?.OrderType ?? "Medication",
            IsLongTerm = task.MedicalOrder?.IsLongTerm ?? false,
            DisplayText = displayText,
            ItemCount = medications.Count,
            InspectionSource = null,
            PatientId = task.PatientId,
            PatientName = task.Patient?.Name ?? "",
            BedId = task.Patient?.BedId ?? "",
            Status = task.Status.ToString(),
            StatusText = GetStatusText(task.Status),
            PlannedStartTime = task.PlannedStartTime,
            PlantEndTime = task.MedicalOrder?.PlantEndTime,
            ContentDescription = contentDesc,
            Medications = medications,
            InspectionInfo = null,
            IsUrgent = isUrgent,
            Remarks = remarks,
            CreateTime = task.CreatedAt,
            AppliedAt = appliedAt,
            AppliedBy = appliedBy,
            ConfirmedAt = confirmedAt
        };
    }

    /// <summary>
    /// 将InspectionOrder映射为ApplicationItemDto
    /// </summary>
    private async Task<ApplicationItemDto> MapInspectionOrderToApplicationItemDto(InspectionOrder order)
    {
        return new ApplicationItemDto
        {
            ApplicationType = "Inspection",
            RelatedId = order.Id,
            OrderId = order.Id,
            OrderType = "Inspection",
            IsLongTerm = order.IsLongTerm,
            DisplayText = order.ItemCode, // TODO: 从字典获取检查项目名称
            ItemCount = 1,
            InspectionSource = order.Source.ToString(),
            PatientId = order.PatientId,
            PatientName = order.Patient?.Name ?? "",
            BedId = order.Patient?.BedId ?? "",
            Status = "Applying", // 检查医嘱还未提交申请时显示为待申请
            StatusText = "待申请",
            PlannedStartTime = order.AppointmentTime ?? order.CreateTime,
            PlantEndTime = order.PlantEndTime,
            ContentDescription = $"检查：{order.ItemCode}",
            Medications = null,
            InspectionInfo = new InspectionDetail
            {
                ItemCode = order.ItemCode,
                ItemName = order.ItemCode, // TODO: 从字典获取检查项目名称
                Location = order.Location,
                Source = order.Source.ToString(),
                Precautions = order.Precautions,
                AppointmentTime = order.AppointmentTime,
                AppointmentPlace = order.AppointmentPlace
            },
            IsUrgent = false,
            Remarks = order.Remarks,
            CreateTime = order.CreateTime,
            AppliedAt = null,
            AppliedBy = null,
            ConfirmedAt = null
        };
    }

    /// <summary>
    /// 获取状态中文描述
    /// </summary>
    private string GetStatusText(ExecutionTaskStatus status)
    {
        return status switch
        {
            ExecutionTaskStatus.Applying => "待申请",
            ExecutionTaskStatus.Applied => "已申请",
            ExecutionTaskStatus.AppliedConfirmed => "就绪/已确认",
            ExecutionTaskStatus.Pending => "待执行",
            ExecutionTaskStatus.InProgress => "执行中",
            ExecutionTaskStatus.Completed => "已完成",
            ExecutionTaskStatus.OrderStopping => "停嘱锁定",
            ExecutionTaskStatus.Stopped => "已停止/作废",
            ExecutionTaskStatus.Incomplete => "异常/拒绝",
            _ => status.ToString()
        };
    }

    #endregion
}
