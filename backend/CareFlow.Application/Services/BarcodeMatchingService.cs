using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services;

/// <summary>
/// 条形码匹配服务实现
/// </summary>
public class BarcodeMatchingService : IBarcodeMatchingService
{
    private readonly IBarcodeService _barcodeService;
    private readonly IRepository<ExecutionTask, long> _executionTaskRepository;
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IRepository<Nurse, string> _nurseRepository;
    private readonly ILogger<BarcodeMatchingService> _logger;

    public BarcodeMatchingService(
        IBarcodeService barcodeService,
        IRepository<ExecutionTask, long> executionTaskRepository,
        IRepository<Patient, string> patientRepository,
        IRepository<Nurse, string> nurseRepository,
        ILogger<BarcodeMatchingService> logger)
    {
        _barcodeService = barcodeService;
        _executionTaskRepository = executionTaskRepository;
        _patientRepository = patientRepository;
        _nurseRepository = nurseRepository;
        _logger = logger;
    }

    public async Task<BarcodeMatchingResult> ValidateBarcodeMatchAsync(
        Stream executionTaskBarcodeImage, 
        Stream patientBarcodeImage,
        string currentNurseId,
        int toleranceMinutes = 30)
    {
        var result = new BarcodeMatchingResult
        {
            ScanTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("开始验证条形码匹配，护士ID: {NurseId}, 允许时间偏差: {ToleranceMinutes} 分钟", currentNurseId, toleranceMinutes);

            // 第一步：验证护士是否存在
            var nurse = await ValidateNurseAsync(currentNurseId, result);
            if (nurse == null)
            {
                return result;
            }

            // 第二步：解码执行任务条形码
            var executionTaskBarcode = await DecodeExecutionTaskBarcodeAsync(executionTaskBarcodeImage, result);
            if (executionTaskBarcode == null)
            {
                return result;
            }

            // 第三步：解码患者条形码
            var patientBarcode = await DecodePatientBarcodeAsync(patientBarcodeImage, result);
            if (patientBarcode == null)
            {
                return result;
            }

            // 第四步：获取执行任务详情
            var executionTask = await GetExecutionTaskAsync(executionTaskBarcode.RecordId, result);
            if (executionTask == null)
            {
                return result;
            }

            // 第五步：验证患者是否存在
            var patient = await GetPatientAsync(patientBarcode.RecordId, result);
            if (patient == null)
            {
                return result;
            }

            // 第六步：执行所有验证逻辑
            await PerformValidationAsync(executionTask, patient, result, toleranceMinutes);
            
            // 第七步：如果验证成功，更新ExecutionTask
            if (result.IsMatched)
            {
                await UpdateExecutionTaskAsync(executionTask, nurse.Id, result);
            }

            _logger.LogInformation("条形码匹配验证完成，结果: {IsMatched}", result.IsMatched);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "条形码匹配验证过程中发生错误");
            result.IsMatched = false;
            result.ErrorMessage = $"验证过程中发生错误: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 解码执行任务条形码
    /// </summary>
    private async Task<BarcodeIndex?> DecodeExecutionTaskBarcodeAsync(Stream imageStream, BarcodeMatchingResult result)
    {
        try
        {
            var barcodeIndex = _barcodeService.RecognizeBarcode(imageStream);
            result.ValidationDetails.ExecutionTaskBarcodeDecoded = true;
            result.ValidationDetails.DecodedExecutionTaskId = barcodeIndex.RecordId;

            _logger.LogDebug("成功解码执行任务条形码: 表={TableName}, ID={RecordId}", 
                barcodeIndex.TableName, barcodeIndex.RecordId);

            // 验证是否为执行任务条形码
            if (!string.Equals(barcodeIndex.TableName, "ExecutionTasks", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"期望的是执行任务条形码，但解码出的是 {barcodeIndex.TableName} 表的条形码";
                _logger.LogWarning("执行任务条形码类型错误: {TableName}", barcodeIndex.TableName);
                return null;
            }

            return barcodeIndex;
        }
        catch (Exception ex)
        {
            result.ValidationDetails.ExecutionTaskBarcodeDecoded = false;
            result.ErrorMessage = $"执行任务条形码解码失败: {ex.Message}";
            _logger.LogError(ex, "执行任务条形码解码失败");
            return null;
        }
    }

    /// <summary>
    /// 解码患者条形码
    /// </summary>
    private async Task<BarcodeIndex?> DecodePatientBarcodeAsync(Stream imageStream, BarcodeMatchingResult result)
    {
        try
        {
            var barcodeIndex = _barcodeService.RecognizeBarcode(imageStream);
            result.ValidationDetails.PatientBarcodeDecoded = true;
            result.ValidationDetails.DecodedPatientId = barcodeIndex.RecordId;

            _logger.LogDebug("成功解码患者条形码: 表={TableName}, ID={RecordId}", 
                barcodeIndex.TableName, barcodeIndex.RecordId);

            // 验证是否为患者条形码
            if (!string.Equals(barcodeIndex.TableName, "Patients", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"期望的是患者条形码，但解码出的是 {barcodeIndex.TableName} 表的条形码";
                _logger.LogWarning("患者条形码类型错误: {TableName}", barcodeIndex.TableName);
                return null;
            }

            return barcodeIndex;
        }
        catch (Exception ex)
        {
            result.ValidationDetails.PatientBarcodeDecoded = false;
            result.ErrorMessage = $"患者条形码解码失败: {ex.Message}";
            _logger.LogError(ex, "患者条形码解码失败");
            return null;
        }
    }

    /// <summary>
    /// 获取执行任务详情
    /// </summary>
    private async Task<ExecutionTask?> GetExecutionTaskAsync(string executionTaskId, BarcodeMatchingResult result)
    {
        try
        {
            if (!long.TryParse(executionTaskId, out long taskId))
            {
                result.ErrorMessage = $"执行任务ID格式无效: {executionTaskId}";
                _logger.LogWarning("执行任务ID格式无效: {ExecutionTaskId}", executionTaskId);
                return null;
            }

            var executionTask = await _executionTaskRepository.GetByIdAsync(taskId);
            if (executionTask == null)
            {
                result.ValidationDetails.ExecutionTaskExists = false;
                result.ErrorMessage = $"执行任务不存在: {executionTaskId}";
                _logger.LogWarning("执行任务不存在: {ExecutionTaskId}", executionTaskId);
                return null;
            }

            result.ValidationDetails.ExecutionTaskExists = true;
            result.ExecutionTaskId = executionTask.Id;
            result.PlannedStartTime = executionTask.PlannedStartTime;

            _logger.LogDebug("成功找到执行任务: ID={TaskId}, 患者ID={PatientId}, 计划时间={PlannedTime}", 
                executionTask.Id, executionTask.PatientId, executionTask.PlannedStartTime);

            return executionTask;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"获取执行任务时发生错误: {ex.Message}";
            _logger.LogError(ex, "获取执行任务时发生错误: {ExecutionTaskId}", executionTaskId);
            return null;
        }
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    private async Task<Patient?> GetPatientAsync(string patientId, BarcodeMatchingResult result)
    {
        try
        {
            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                result.ValidationDetails.PatientExists = false;
                result.ErrorMessage = $"患者不存在: {patientId}";
                _logger.LogWarning("患者不存在: {PatientId}", patientId);
                return null;
            }

            result.ValidationDetails.PatientExists = true;
            result.PatientId = patient.Id;

            _logger.LogDebug("成功找到患者: ID={PatientId}, 姓名={PatientName}", 
                patient.Id, patient.Name);

            return patient;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"获取患者信息时发生错误: {ex.Message}";
            _logger.LogError(ex, "获取患者信息时发生错误: {PatientId}", patientId);
            return null;
        }
    }

    /// <summary>
    /// 执行所有验证逻辑
    /// </summary>
    private async Task PerformValidationAsync(
        ExecutionTask executionTask, 
        Patient patient, 
        BarcodeMatchingResult result, 
        int toleranceMinutes)
    {
        var validationErrors = new List<string>();

        // 验证1: 患者ID是否匹配
        if (string.Equals(executionTask.PatientId, patient.Id, StringComparison.OrdinalIgnoreCase))
        {
            result.ValidationDetails.PatientIdMatched = true;
            _logger.LogDebug("患者ID匹配成功: {PatientId}", patient.Id);
        }
        else
        {
            result.ValidationDetails.PatientIdMatched = false;
            validationErrors.Add($"患者ID不匹配: 执行任务患者ID={executionTask.PatientId}, 扫码患者ID={patient.Id}");
            _logger.LogWarning("患者ID不匹配: 执行任务患者ID={TaskPatientId}, 扫码患者ID={ScannedPatientId}", 
                executionTask.PatientId, patient.Id);
        }

        // 验证2: 执行时间是否在允许范围内
        var timeDifference = Math.Abs((result.ScanTime - executionTask.PlannedStartTime).TotalMinutes);
        result.TimeDifferenceMinutes = timeDifference;

        if (timeDifference <= toleranceMinutes)
        {
            result.ValidationDetails.ExecutionTimeMatched = true;
            _logger.LogDebug("执行时间匹配: 时间差={TimeDifference}分钟, 允许范围={ToleranceMinutes}分钟", 
                timeDifference, toleranceMinutes);
        }
        else
        {
            result.ValidationDetails.ExecutionTimeMatched = false;
            validationErrors.Add($"执行时间超出允许范围: 计划时间={executionTask.PlannedStartTime:yyyy-MM-dd HH:mm:ss}, " +
                                $"当前时间={result.ScanTime:yyyy-MM-dd HH:mm:ss}, " +
                                $"时间差={timeDifference:F1}分钟, 允许范围={toleranceMinutes}分钟");
            _logger.LogWarning("执行时间超出允许范围: 计划时间={PlannedTime}, 当前时间={ScanTime}, 时间差={TimeDifference}分钟", 
                executionTask.PlannedStartTime, result.ScanTime, timeDifference);
        }

        // 验证3: 执行任务状态是否允许执行
        var allowedStatuses = new[] { "Pending", "Running" };
        if (allowedStatuses.Contains(executionTask.Status))
        {
            result.ValidationDetails.ExecutionTaskStatusValid = true;
            _logger.LogDebug("执行任务状态有效: {Status}", executionTask.Status);
        }
        else
        {
            result.ValidationDetails.ExecutionTaskStatusValid = false;
            validationErrors.Add($"执行任务状态不允许执行: 当前状态={executionTask.Status}");
            _logger.LogWarning("执行任务状态不允许执行: {Status}", executionTask.Status);
        }

        // 综合判断是否匹配成功
        result.IsMatched = result.ValidationDetails.PatientIdMatched && 
                          result.ValidationDetails.ExecutionTimeMatched && 
                          result.ValidationDetails.ExecutionTaskStatusValid;

        if (!result.IsMatched)
        {
            result.ErrorMessage = string.Join("; ", validationErrors);
            _logger.LogWarning("条形码匹配验证失败: {ErrorMessage}", result.ErrorMessage);
        }
        else
        {
            _logger.LogInformation("条形码匹配验证成功: 执行任务ID={TaskId}, 患者ID={PatientId}", 
                executionTask.Id, patient.Id);
        }
    }

    /// <summary>
    /// 验证护士是否存在并有效
    /// </summary>
    private async Task<Nurse?> ValidateNurseAsync(string nurseId, BarcodeMatchingResult result)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nurseId))
            {
                result.ErrorMessage = "护士ID不能为空";
                _logger.LogWarning("护士ID为空");
                return null;
            }

            // 首先查找Nurse表中是否存在该ID
            var nurse = await _nurseRepository.GetByIdAsync(nurseId);
            if (nurse == null)
            {
                result.ErrorMessage = $"护士不存在: {nurseId}";
                _logger.LogWarning("护士不存在: {NurseId}", nurseId);
                return null;
            }

            // 验证护士账号是否激活
            if (!nurse.IsActive)
            {
                result.ErrorMessage = $"护士账号已被禁用: {nurseId}";
                _logger.LogWarning("护士账号已被禁用: {NurseId}", nurseId);
                return null;
            }

            result.ExecutorNurseId = nurse.Id;
            _logger.LogDebug("成功验证护士: ID={NurseId}, 姓名={NurseName}, 护士级别={NurseRank}", 
                nurse.Id, nurse.Name, nurse.NurseRank);
            return nurse;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"验证护士信息时发生错误: {ex.Message}";
            _logger.LogError(ex, "验证护士信息时发生错误: {NurseId}", nurseId);
            return null;
        }
    }

    /// <summary>
    /// 更新执行任务的开始时间和执行护士
    /// </summary>
    private async Task UpdateExecutionTaskAsync(ExecutionTask executionTask, string nurseId, BarcodeMatchingResult result)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // 更新执行任务信息
            executionTask.ActualStartTime = startTime;
            executionTask.ExecutorStaffId = nurseId;
            executionTask.Status = "Running";
            executionTask.LastModifiedAt = startTime;

            // 保存到数据库
            await _executionTaskRepository.UpdateAsync(executionTask);

            // 更新结果对象
            result.ActualStartTime = startTime;
            result.ExecutorNurseId = nurseId;

            _logger.LogInformation("成功更新执行任务: 任务ID={TaskId}, 护士ID={NurseId}, 开始时间={StartTime}", 
                executionTask.Id, nurseId, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新执行任务时发生错误: 任务ID={TaskId}, 护士ID={NurseId}", 
                executionTask.Id, nurseId);
            
            // 这里不设置IsMatched为false，因为验证已经成功，只是更新失败
            // 可以考虑添加一个警告字段或者在日志中记录
            result.ErrorMessage = result.ErrorMessage + $"; 警告: 更新任务状态失败 - {ex.Message}";
        }
    }
}