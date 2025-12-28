using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Application.Interfaces;
using CareFlow.Application.Common;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PatientModel = CareFlow.Core.Models.Organization.Patient;

namespace CareFlow.Application.Services.OrderApplication;

/// <summary>
/// 医嘱申请服务实现
/// </summary>
public class OrderApplicationService : IOrderApplicationService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IRepository<InspectionOrder, long> _inspectionOrderRepository;
    private readonly IRepository<MedicationOrder, long> _medicationOrderRepository;
    private readonly IRepository<PatientModel, string> _patientRepository;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
    private readonly IRepository<MedicationReturnRequest, long> _returnRequestRepository;
    private readonly IPharmacyIntegrationService _pharmacyService;
    private readonly IInspectionStationService _inspectionStationService;
    private readonly IInspectionService _inspectionService;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly IBarcodeService _barcodeService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<OrderApplicationService> _logger;

    public OrderApplicationService(
        IRepository<ExecutionTask, long> taskRepository,
        IRepository<InspectionOrder, long> inspectionOrderRepository,
        IRepository<MedicationOrder, long> medicationOrderRepository,
        IRepository<PatientModel, string> patientRepository,
        IRepository<BarcodeIndex, string> barcodeRepository,
        IRepository<MedicationReturnRequest, long> returnRequestRepository,
        IPharmacyIntegrationService pharmacyService,
        IInspectionStationService inspectionStationService,
        IInspectionService inspectionService,
        INurseAssignmentService nurseAssignmentService,
        IBarcodeService barcodeService,
        IBackgroundJobService backgroundJobService,
        ILogger<OrderApplicationService> logger)
    {
        _taskRepository = taskRepository;
        _inspectionOrderRepository = inspectionOrderRepository;
        _medicationOrderRepository = medicationOrderRepository;
        _patientRepository = patientRepository;
        _barcodeRepository = barcodeRepository;
        _returnRequestRepository = returnRequestRepository;
        _pharmacyService = pharmacyService;
        _inspectionStationService = inspectionStationService;
        _inspectionService = inspectionService;
        _nurseAssignmentService = nurseAssignmentService;
        _barcodeService = barcodeService;
        _backgroundJobService = backgroundJobService;
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
                // 默认查询：待申请、已申请、已确认、待退药
                query = query.Where(t => t.Status == ExecutionTaskStatus.Applying 
                                      || t.Status == ExecutionTaskStatus.Applied 
                                      || t.Status == ExecutionTaskStatus.AppliedConfirmed
                                      || t.Status == ExecutionTaskStatus.PendingReturn);
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
            // 查询检查任务（已生成任务的检查医嘱）
            var query = _taskRepository.GetQueryable()
                .Include(t => t.Patient)
                    .ThenInclude(p => p.Bed)
                .Include(t => t.MedicalOrder)
                .Where(t => request.PatientIds.Contains(t.PatientId)
                         && t.MedicalOrder.OrderType == "InspectionOrder"
                         && (t.Status == ExecutionTaskStatus.Applying 
                             || t.Status == ExecutionTaskStatus.Applied 
                             || t.Status == ExecutionTaskStatus.AppliedConfirmed));

            // 状态筛选
            if (request.StatusFilter != null && request.StatusFilter.Any())
            {
                var statusEnums = request.StatusFilter
                    .Select(s => Enum.Parse<ExecutionTaskStatus>(s))
                    .ToList();
                query = query.Where(t => statusEnums.Contains(t.Status));
            }

            var tasks = await query
                .OrderBy(t => t.PlannedStartTime)
                .ToListAsync();

            _logger.LogInformation("查询到 {Count} 条检查任务", tasks.Count);

            // 转换为DTO
            var result = new List<ApplicationItemDto>();
            foreach (var task in tasks)
            {
                try
                {
                    var dto = await MapInspectionTaskToApplicationItemDto(task);
                    result.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "映射检查任务 {TaskId} 失败", task.Id);
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
                        }, JsonConfig.DefaultOptions);

                        task.DataPayload = JsonSerializer.Serialize(payload, JsonConfig.DefaultOptions);
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
        _logger.LogInformation("护士ID: {NurseId}, 任务数: {Count}, 加急: {IsUrgent}",
            request.NurseId, request.TaskIds.Count, request.IsUrgent);

        var processedOrderIds = new List<long>();
        var errors = new List<string>();

        try
        {
            // 1. 查找待申请的检查申请任务
            foreach (var taskId in request.TaskIds)
            {
                // 查找申请任务（签收时生成的任务）
                var applicationTask = await _taskRepository.GetByIdAsync(taskId);
                
                if (applicationTask == null)
                {
                    _logger.LogWarning("❌ 申请任务 {TaskId} 不存在", taskId);
                    errors.Add($"申请任务 {taskId} 不存在");
                    continue;
                }

                if (applicationTask.Status != ExecutionTaskStatus.Applying)
                {
                    _logger.LogWarning("❌ 申请任务 {TaskId} 状态为 {Status}，不能申请", taskId, applicationTask.Status);
                    errors.Add($"申请任务 {taskId} 状态为 {applicationTask.Status}，不能申请");
                    continue;
                }

                _logger.LogInformation("📋 找到申请任务 TaskId={TaskId}, OrderId={OrderId}, Status={Status}", 
                    applicationTask.Id, applicationTask.MedicalOrderId, applicationTask.Status);

                // 2. 更新任务状态为Applied（已申请）
                applicationTask.Status = ExecutionTaskStatus.Applied;
                applicationTask.LastModifiedAt = DateTime.UtcNow;
                
                // 注意：对于检查类医嘱（ApplicationWithPrint），不设置实际开始时间和执行护士
                // 因为提交申请只是预约，真正的执行是打印导引单时
                // 对于药品医嘱（Verification），这里可以记录申请信息
                if (applicationTask.Category != TaskCategory.ApplicationWithPrint)
                {
                    applicationTask.ActualStartTime = DateTime.UtcNow;  // 记录提交时间
                    applicationTask.ExecutorStaffId = request.NurseId;  // 记录提交护士
                }
                
                // 更新DataPayload添加申请信息
                try
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(applicationTask.DataPayload);
                    if (payload != null)
                    {
                        payload["SubmittedAt"] = JsonSerializer.SerializeToElement(DateTime.UtcNow, JsonConfig.DefaultOptions);
                        payload["SubmittedBy"] = JsonSerializer.SerializeToElement(request.NurseId, JsonConfig.DefaultOptions);
                        payload["IsUrgent"] = JsonSerializer.SerializeToElement(request.IsUrgent, JsonConfig.DefaultOptions);
                        applicationTask.DataPayload = JsonSerializer.Serialize(payload, JsonConfig.DefaultOptions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "更新任务 {TaskId} 的DataPayload失败", taskId);
                }
                
                await _taskRepository.UpdateAsync(applicationTask);
                processedOrderIds.Add(applicationTask.MedicalOrderId);
                _logger.LogInformation("✅ 申请任务 {TaskId} 已更新为Applied状态", applicationTask.Id);
            }

            if (processedOrderIds.Count == 0)
            {
                return new ApplicationResponseDto
                {
                    Success = false,
                    Message = "所有检查申请失败：未找到待申请的任务",
                    Errors = errors
                };
            }

            // 3. 调用检查站系统接口（传递医嘱ID列表）
            var inspectionResult = await _inspectionStationService.SendInspectionRequestAsync(
                processedOrderIds, request.IsUrgent);

            if (!inspectionResult.Success)
            {
                _logger.LogWarning("⚠️ 检查站系统接口调用失败: {Message}", inspectionResult.Message);
                return new ApplicationResponseDto
                {
                    Success = false,
                    Message = $"检查站系统接口调用失败: {inspectionResult.Message}",
                    ProcessedIds = processedOrderIds,
                    Errors = errors
                };
            }

            // 3.5 检查站确认成功后，更新申请任务状态为 AppliedConfirmed
            _logger.LogInformation("🔄 更新申请任务状态为已确认...");
            foreach (var taskId in request.TaskIds)
            {
                var applicationTask = await _taskRepository.GetByIdAsync(taskId);
                if (applicationTask == null)
                {
                    _logger.LogWarning("⚠️ 申请任务 {TaskId} 不存在，无法更新状态", taskId);
                    errors.Add($"申请任务 {taskId} 不存在");
                    continue;
                }

                // 严格验证：只有Applied状态的任务才能更新为AppliedConfirmed
                if (applicationTask.Status != ExecutionTaskStatus.Applied)
                {
                    _logger.LogWarning("⚠️ 申请任务 {TaskId} 状态为 {Status}，不是Applied状态，无法确认", 
                        taskId, applicationTask.Status);
                    errors.Add($"申请任务 {taskId} 状态为 {applicationTask.Status}，必须为Applied状态才能确认");
                    continue;
                }

                applicationTask.Status = ExecutionTaskStatus.AppliedConfirmed;
                applicationTask.LastModifiedAt = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(applicationTask);
                _logger.LogInformation("✅ 申请任务 {TaskId} 状态已更新为 AppliedConfirmed", taskId);
            }

            // 4. 预约成功后，生成任务、分配护士、生成条形码
            if (inspectionResult.AppointmentDetails != null && inspectionResult.AppointmentDetails.Any())
            {
                _logger.LogInformation("🔄 开始生成检查任务...");
                
                foreach (var (orderId, appointmentDetail) in inspectionResult.AppointmentDetails)
                {
                    try
                    {
                        // 4.1 生成检查执行任务（2个任务：签到、完成确认）
                        var tasks = await _inspectionService.GenerateExecutionTasksAsync(
                            orderId, appointmentDetail);
                        
                        _logger.LogInformation("✅ 检查医嘱 {OrderId} 生成了 {Count} 个执行任务", 
                            orderId, tasks.Count);

                        // 4.2 为每个任务分配责任护士并生成条形码
                        foreach (var task in tasks)
                        {
                            // 根据任务计划时间分配责任护士
                            var order = await _inspectionOrderRepository.GetByIdAsync(orderId);
                            if (order != null)
                            {
                                var responsibleNurse = await _nurseAssignmentService
                                    .CalculateResponsibleNurseAsync(order.PatientId, task.PlannedStartTime);

                                if (responsibleNurse != null)
                                {
                                    task.AssignedNurseId = responsibleNurse;
                                    _logger.LogInformation("任务 {TaskId} 分配计划责任护士 {NurseId}", 
                                        task.Id, responsibleNurse);
                                }
                                else
                                {
                                    _logger.LogWarning("任务 {TaskId} 计划时间 {Time} 无排班护士，计划责任护士留空",
                                        task.Id, task.PlannedStartTime);
                                }
                            }

                            await _taskRepository.UpdateAsync(task);
                        }

                        // 4.3 为每个任务生成条形码
                        int barcodeSuccessCount = 0;
                        foreach (var task in tasks)
                        {
                            try
                            {
                                await GenerateBarcodeForTaskAsync(task);
                                barcodeSuccessCount++;
                                _logger.LogInformation("✅ 任务 {TaskId} 已生成条形码", task.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "为任务 {TaskId} 生成条形码失败", task.Id);
                                // 条形码生成失败不影响整体流程
                            }
                        }
                        
                        _logger.LogInformation("✅ 检查医嘱 {OrderId} 生成了 {Count} 个任务条形码", 
                            orderId, barcodeSuccessCount);
                        
                        // 注意：检查报告现在会在任务完成后3分钟自动生成
                        // 不再使用预约时间作为触发时机
                        
                        _logger.LogInformation("✅ 检查医嘱 {OrderId} 预约确认完成，报告将在任务完成后3分钟生成", orderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ 处理检查医嘱 {OrderId} 的任务生成失败", orderId);
                        errors.Add($"检查医嘱 {orderId} 任务生成失败: {ex.Message}");
                    }
                }
            }

            return new ApplicationResponseDto
            {
                Success = true,
                Message = errors.Count > 0
                    ? $"成功申请 {processedOrderIds.Count} 个检查，失败 {errors.Count} 个"
                    : $"成功申请 {processedOrderIds.Count} 个检查，并已生成执行任务",
                ProcessedIds = processedOrderIds,
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
                // 转换为北京时间显示
                var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var beijingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaTimeZone);
                task.ExceptionReason = $"[{beijingTime:yyyy-MM-dd HH:mm}] 护士{nurseId}撤销申请: {reason ?? "无"}";

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

                // 记录撤销信息（转换为北京时间显示）
                var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var beijingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaTimeZone);
                order.Remarks += $"\n[{beijingTime:yyyy-MM-dd HH:mm}] 护士{nurseId}撤销申请: {reason ?? "无"}";

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
        bool isDischargeOrder = false;
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

            // 检查是否为出院医嘱任务
            if (payload != null && payload.ContainsKey("IsDischargeOrder"))
            {
                isDischargeOrder = payload["IsDischargeOrder"].GetBoolean();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析DataPayload失败，任务ID: {TaskId}", task.Id);
        }

        // 获取药品信息和医嘱详情
        var medications = new List<MedicationItemDetail>();
        var medOrder = task.MedicalOrder as MedicationOrder;
        var surgicalOrder = task.MedicalOrder as SurgicalOrder;
        var dischargeOrder = task.MedicalOrder as DischargeOrder;
        
        // 从药品医嘱获取药品信息
        if (medOrder != null && medOrder.Items != null)
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
        // 从手术医嘱获取药品信息（手术医嘱也继承了 Items 属性）
        else if (surgicalOrder != null && surgicalOrder.Items != null)
        {
            foreach (var item in surgicalOrder.Items)
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
        // 从出院医嘱获取带回药品信息
        else if (dischargeOrder != null && dischargeOrder.Items != null)
        {
            foreach (var item in dischargeOrder.Items)
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

        // 构建显示文本：多药品时显示第一个 + "等"，出院医嘱加上标识
        string displayText;
        if (medications.Count > 1)
        {
            displayText = isDischargeOrder 
                ? $"{medications[0].DrugName}等（出院带回）" 
                : $"{medications[0].DrugName}等";
        }
        else if (medications.Count == 1)
        {
            displayText = isDischargeOrder 
                ? $"{medications[0].DrugName}（出院带回）" 
                : medications[0].DrugName;
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
            IsDischargeOrder = isDischargeOrder, // 标记是否为出院医嘱
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
            // 填充时间策略和用法信息
            TimingStrategy = medOrder?.TimingStrategy,
            UsageRoute = medOrder?.UsageRoute.ToString(),
            IntervalHours = medOrder?.IntervalHours,
            IntervalDays = medOrder?.IntervalDays,
            SmartSlotsMask = medOrder?.SmartSlotsMask,
            // 填充手术信息（如果是手术类医嘱）
            SurgeryName = surgicalOrder?.SurgeryName,
            SurgeryScheduleTime = surgicalOrder?.ScheduleTime,
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
    /// 将检查任务映射为ApplicationItemDto
    /// </summary>
    private async Task<ApplicationItemDto> MapInspectionTaskToApplicationItemDto(ExecutionTask task)
    {
        var inspectionOrder = task.MedicalOrder as InspectionOrder;
        if (inspectionOrder == null)
        {
            throw new InvalidOperationException($"任务 {task.Id} 的医嘱不是检查医嘱类型");
        }

        return new ApplicationItemDto
        {
            ApplicationType = "Inspection",
            RelatedId = task.Id, // 使用任务ID
            OrderId = inspectionOrder.Id,
            OrderType = "Inspection",
            IsLongTerm = inspectionOrder.IsLongTerm,
            DisplayText = inspectionOrder.ItemName,
            ItemCount = 1,
            InspectionSource = inspectionOrder.Source.ToString(),
            PatientId = task.PatientId,
            PatientName = task.Patient?.Name ?? "",
            BedId = task.Patient?.Bed?.Id ?? "",
            Status = task.Status.ToString(), // 从任务状态读取
            StatusText = GetStatusText(task.Status), // 从任务状态转换
            PlannedStartTime = task.PlannedStartTime,
            PlantEndTime = inspectionOrder.PlantEndTime,
            ContentDescription = $"检查：{inspectionOrder.ItemName}",
            Medications = null,
            InspectionInfo = new InspectionDetail
            {
                ItemCode = inspectionOrder.ItemCode,
                ItemName = inspectionOrder.ItemName,
                Location = inspectionOrder.Location,
                Source = inspectionOrder.Source.ToString(),
                Precautions = inspectionOrder.Precautions,
                AppointmentTime = inspectionOrder.AppointmentTime,
                AppointmentPlace = inspectionOrder.AppointmentPlace
            },
            IsUrgent = false,
            Remarks = inspectionOrder.Remarks,
            CreateTime = task.CreateTime,
            AppliedAt = task.Status >= ExecutionTaskStatus.Applied ? task.LastModifiedAt : null,
            AppliedBy = task.AssignedNurseId,
            ConfirmedAt = task.Status == ExecutionTaskStatus.AppliedConfirmed ? task.LastModifiedAt : null
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
            ExecutionTaskStatus.PendingReturn => "待退药",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// 为任务生成条形码索引和图片
    /// </summary>
    private async Task GenerateBarcodeForTaskAsync(ExecutionTask task)
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
            throw;
        }
    }

    #endregion

    #region 退药相关方法

    /// <summary>
    /// 申请退药（AppliedConfirmed状态，护士主动退药）
    /// </summary>
    public async Task<ApplicationResponseDto> RequestReturnMedicationAsync(
        long taskId, string nurseId, string? reason = null)
    {
        _logger.LogInformation("========== 护士申请退药 ==========");
        _logger.LogInformation("任务ID: {TaskId}, 护士ID: {NurseId}, 原因: {Reason}",
            taskId, nurseId, reason);

        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            
            if (task == null)
            {
                _logger.LogWarning("❌ 任务 {TaskId} 不存在", taskId);
                throw new Exception($"任务 {taskId} 不存在");
            }

            if (task.Status != ExecutionTaskStatus.AppliedConfirmed)
            {
                _logger.LogWarning("❌ 任务 {TaskId} 状态为 {Status}，只能退回已确认状态的药品", 
                    taskId, task.Status);
                throw new Exception($"任务状态为 {task.Status}，只能退回已确认状态的药品");
            }

            // 1. 创建退药记录
            var returnRequest = new MedicationReturnRequest
            {
                ExecutionTaskId = taskId,
                ReturnType = "ManualCancel",
                RequestedBy = nurseId,
                RequestedAt = DateTime.UtcNow,
                Reason = reason ?? "护士主动退药",
                Status = "Pending"
            };
            await _returnRequestRepository.AddAsync(returnRequest);
            
            _logger.LogInformation("✅ 已创建退药记录 {RequestId}", returnRequest.Id);

            // 2. 更新任务状态为待退药
            task.Status = ExecutionTaskStatus.PendingReturn;
            task.LastModifiedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task);
            
            _logger.LogInformation("✅ 任务 {TaskId} 状态已更新为 PendingReturn", taskId);

            // 3. 调用药房退药接口
            returnRequest.Status = "Submitted";
            returnRequest.SubmittedAt = DateTime.UtcNow;
            
            var result = await _pharmacyService.ReturnMedicationAsync(taskId);
            
            if (result.Success)
            {
                returnRequest.Status = "Confirmed";
                returnRequest.ConfirmedAt = DateTime.UtcNow;
                returnRequest.PharmacyResponse = result.Message;
                
                // 退药成功，恢复为待申请
                task.Status = ExecutionTaskStatus.Applying;
                task.LastModifiedAt = DateTime.UtcNow;
                
                _logger.LogInformation("✅ 退药成功，任务 {TaskId} 状态恢复为 Applying", taskId);
            }
            else
            {
                returnRequest.Status = "Failed";
                returnRequest.PharmacyResponse = result.Message;
                
                _logger.LogWarning("⚠️ 退药失败: {Message}", result.Message);
            }
            
            await _returnRequestRepository.UpdateAsync(returnRequest);
            await _taskRepository.UpdateAsync(task);

            return new ApplicationResponseDto 
            { 
                Success = result.Success,
                Message = result.Success ? "退药成功" : $"退药失败: {result.Message}",
                ProcessedIds = result.Success ? new List<long> { taskId } : new List<long>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 申请退药失败");
            throw;
        }
    }

    /// <summary>
    /// 确认退药（PendingReturn状态，护士确认执行退药）
    /// </summary>
    public async Task<ApplicationResponseDto> ConfirmReturnMedicationAsync(
        long taskId, string nurseId)
    {
        _logger.LogInformation("========== 护士确认退药 ==========");
        _logger.LogInformation("任务ID: {TaskId}, 护士ID: {NurseId}", taskId, nurseId);

        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            
            if (task == null)
            {
                _logger.LogWarning("❌ 任务 {TaskId} 不存在", taskId);
                throw new Exception($"任务 {taskId} 不存在");
            }

            if (task.Status != ExecutionTaskStatus.PendingReturn)
            {
                _logger.LogWarning("❌ 任务 {TaskId} 状态为 {Status}，只能确认待退药状态的任务", 
                    taskId, task.Status);
                throw new Exception($"任务状态为 {task.Status}，只能确认待退药状态的任务");
            }

            // 查找待处理的退药记录
            var returnRequest = await _returnRequestRepository.GetQueryable()
                .FirstOrDefaultAsync(r => r.ExecutionTaskId == taskId 
                                       && r.Status == "Pending");
            
            if (returnRequest == null)
            {
                _logger.LogWarning("❌ 未找到待处理的退药申请记录");
                throw new Exception("未找到待处理的退药申请记录");
            }

            _logger.LogInformation("📋 找到退药记录 {RequestId}，退药类型: {ReturnType}", 
                returnRequest.Id, returnRequest.ReturnType);

            // 提交到药房
            returnRequest.Status = "Submitted";
            returnRequest.SubmittedAt = DateTime.UtcNow;
            
            var result = await _pharmacyService.ReturnMedicationAsync(taskId);
            
            if (result.Success)
            {
                returnRequest.Status = "Confirmed";
                returnRequest.ConfirmedAt = DateTime.UtcNow;
                returnRequest.PharmacyResponse = result.Message;
                
                // 退药成功，任务改为已停止
                task.Status = ExecutionTaskStatus.Stopped;
                task.LastModifiedAt = DateTime.UtcNow;
                
                _logger.LogInformation("✅ 退药成功，任务 {TaskId} 状态改为 Stopped", taskId);
            }
            else
            {
                returnRequest.Status = "Failed";
                returnRequest.PharmacyResponse = result.Message;
                
                _logger.LogWarning("⚠️ 退药失败: {Message}", result.Message);
            }
            
            await _returnRequestRepository.UpdateAsync(returnRequest);
            await _taskRepository.UpdateAsync(task);

            return new ApplicationResponseDto 
            { 
                Success = result.Success,
                Message = result.Success ? "退药确认成功" : $"退药确认失败: {result.Message}",
                ProcessedIds = result.Success ? new List<long> { taskId } : new List<long>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认退药失败");
            throw;
        }
    }

    /// <summary>
    /// 确认异常取消退药（PendingReturnCancelled状态，将任务改为Incomplete）
    /// </summary>
    public async Task<ApplicationResponseDto> ConfirmCancelledReturnAsync(
        long taskId, string nurseId)
    {
        _logger.LogInformation("========== 护士确认异常取消退药 ==========");
        _logger.LogInformation("任务ID: {TaskId}, 护士ID: {NurseId}", taskId, nurseId);

        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            
            if (task == null)
            {
                _logger.LogWarning("❌ 任务 {TaskId} 不存在", taskId);
                throw new Exception($"任务 {taskId} 不存在");
            }

            if (task.Status != ExecutionTaskStatus.PendingReturnCancelled)
            {
                _logger.LogWarning("❌ 任务 {TaskId} 状态为 {Status}，只能确认PendingReturnCancelled状态的任务", 
                    taskId, task.Status);
                throw new Exception($"任务状态为 {task.Status}，只能确认异常取消待退药状态的任务");
            }

            _logger.LogInformation("✅ 任务状态确认，将任务 {TaskId} 改为 Incomplete", taskId);

            // 直接将任务改为Incomplete状态
            task.Status = ExecutionTaskStatus.Incomplete;
            task.LastModifiedAt = DateTime.UtcNow;
            
            await _taskRepository.UpdateAsync(task);

            _logger.LogInformation("✅ 异常取消退药确认成功，任务 {TaskId} 状态改为 Incomplete", taskId);

            return new ApplicationResponseDto 
            { 
                Success = true,
                Message = "确认成功，任务已标记为异常状态",
                ProcessedIds = new List<long> { taskId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认异常取消退药失败");
            throw;
        }
    }

    #endregion
}
