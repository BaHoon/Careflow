using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Application.Services;

/// <summary>
/// 条形码匹配服务实现
/// </summary>
public class BarcodeMatchingService : IBarcodeMatchingService
{
    private readonly IBarcodeService _barcodeService;
    private readonly IRepository<ExecutionTask, long> _executionTaskRepository;
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IRepository<MedicationOrder, long> _medicationOrderRepository;
    private readonly ILogger<BarcodeMatchingService> _logger;

    public BarcodeMatchingService(
        IBarcodeService barcodeService,
        IRepository<ExecutionTask, long> executionTaskRepository,
        IRepository<Patient, string> patientRepository,
        IRepository<MedicationOrder, long> medicationOrderRepository,
        ILogger<BarcodeMatchingService> logger)
    {
        _barcodeService = barcodeService;
        _executionTaskRepository = executionTaskRepository;
        _patientRepository = patientRepository;
        _medicationOrderRepository = medicationOrderRepository;
        _logger = logger;
    }

    public async Task<BarcodeMatchingResult> ValidateBarcodeMatchAsync(
        Stream executionTaskBarcodeImage, 
        Stream patientBarcodeImage, 
        int toleranceMinutes = 30,
        bool checkTime = true)
    {
        var result = new BarcodeMatchingResult
        {
            ScanTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("开始验证条形码匹配，允许时间偏差: {ToleranceMinutes} 分钟", toleranceMinutes);

            // 第一步：解码执行任务条形码
            var executionTaskBarcode = await DecodeExecutionTaskBarcodeAsync(executionTaskBarcodeImage, result);
            if (executionTaskBarcode == null)
            {
                return result;
            }

            // 第二步：解码患者条形码
            var patientBarcode = await DecodePatientBarcodeAsync(patientBarcodeImage, result);
            if (patientBarcode == null)
            {
                return result;
            }

            // 第三步：获取执行任务详情
            var executionTask = await GetExecutionTaskAsync(executionTaskBarcode.RecordId, result);
            if (executionTask == null)
            {
                return result;
            }

            // 验证任务类型是否允许扫码匹配患者
            var allowedCategories = new[] { TaskCategory.Immediate, TaskCategory.Duration, TaskCategory.ResultPending };
            if (!allowedCategories.Contains(executionTask.Category))
            {
                result.IsMatched = false;
                result.ErrorMessage = $"该任务类型({executionTask.Category})不支持扫码匹配患者";
                return result;
            }

            // 第四步：验证患者是否存在
            var patient = await GetPatientAsync(patientBarcode.RecordId, result);
            if (patient == null)
            {
                return result;
            }

            // 第五步：执行所有验证逻辑
            await PerformValidationAsync(executionTask, patient, result, toleranceMinutes, checkTime);

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

    public async Task<TaskDrugMatchingResult> ValidateTaskDrugMatchAsync(
        Stream executionTaskBarcodeImage,
        Stream drugBarcodeImage)
    {
        var result = new TaskDrugMatchingResult();
        
        try
        {
            // 1. 解码任务条形码
            var taskBarcode = _barcodeService.RecognizeBarcode(executionTaskBarcodeImage);
            if (taskBarcode == null || taskBarcode.TableName != "ExecutionTasks")
            {
                result.ErrorMessage = "无效的任务条形码";
                return result;
            }
            
            if (!long.TryParse(taskBarcode.RecordId, out long taskId))
            {
                result.ErrorMessage = "任务ID格式错误";
                return result;
            }
            result.TaskId = taskId;

            // 2. 解码药品条形码
            var drugBarcode = _barcodeService.RecognizeBarcode(drugBarcodeImage);
            if (drugBarcode == null || drugBarcode.TableName != "Drugs")
            {
                result.ErrorMessage = "无效的药品条形码";
                return result;
            }
            result.ScannedDrugId = drugBarcode.RecordId;

            // 3. 获取任务详情
            var task = await _executionTaskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                result.ErrorMessage = "任务不存在";
                return result;
            }

            // 验证任务类型是否允许扫码匹配药品
            if (task.Category != TaskCategory.Verification)
            {
                result.ErrorMessage = $"该任务类型({task.Category})不支持扫码匹配药品";
                return result;
            }

            // 4. 获取医嘱详情 (包含药品列表)
            var order = await _medicationOrderRepository.GetQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == task.MedicalOrderId);
                
            if (order == null)
            {
                result.ErrorMessage = "关联医嘱不存在";
                return result;
            }

            // 5. 验证药品是否属于该医嘱
            var orderItem = order.Items?.FirstOrDefault(i => i.DrugId == result.ScannedDrugId);
            if (orderItem == null)
            {
                result.ErrorMessage = "该药品不属于当前医嘱";
                return result;
            }
            
            // 6. 更新任务状态 (DataPayload)
            if (!string.IsNullOrEmpty(task.DataPayload))
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var payload = JsonSerializer.Deserialize<JsonElement>(task.DataPayload, jsonOptions);
                
                // 我们需要修改 JSON 结构，所以转为可编辑的对象
                // 这里简化处理：解析为 Dictionary/List 结构进行修改
                // 注意：为了简单起见，这里假设 DataPayload 结构是固定的
                
                // 使用 System.Text.Json.Nodes (需要 .NET 6+) 或者重新序列化为特定对象
                // 这里我们定义一个临时的 DTO 来反序列化
                var payloadObj = JsonSerializer.Deserialize<TaskPayloadDto>(task.DataPayload, jsonOptions);
                
                if (payloadObj != null && payloadObj.Items != null)
                {
                    var targetItem = payloadObj.Items.FirstOrDefault(i => i.DrugId == result.ScannedDrugId);
                    if (targetItem != null)
                    {
                        result.ScannedDrugName = targetItem.Text;
                        
                        if (targetItem.IsChecked)
                        {
                            // 已经扫描过，返回提示信息但不报错
                            result.ErrorMessage = $"药品 {result.ScannedDrugName} 已经核对过了";
                            // 依然算作匹配成功，只是不更新状态
                            result.IsMatched = true;
                        }
                        else
                        {
                            targetItem.IsChecked = true;
                            
                            // 保存回数据库
                            task.DataPayload = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions 
                            { 
                                WriteIndented = false,
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            
                            await _executionTaskRepository.UpdateAsync(task);
                            result.UpdatedDataPayload = task.DataPayload;
                            result.IsMatched = true;
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "该药品不在当前任务的核对清单中";
                    }
                    
                    // 计算进度
                    var drugItems = payloadObj.Items.Where(i => !string.IsNullOrEmpty(i.DrugId)).ToList();
                    result.TotalDrugs = drugItems.Count;
                    result.ConfirmedDrugs = drugItems.Count(i => i.IsChecked);
                    result.IsFullyCompleted = result.TotalDrugs > 0 && result.TotalDrugs == result.ConfirmedDrugs;
                    result.IsMatched = true;
                }
                else
                {
                    result.ErrorMessage = "任务数据格式异常";
                }
            }
            else
            {
                result.ErrorMessage = "任务数据为空";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药品匹配验证失败");
            result.ErrorMessage = $"系统错误: {ex.Message}";
        }
        
        return result;
    }

    public async Task<ExecutionTask?> FindNearestDataCollectionTaskAsync(Stream patientBarcodeImage)
    {
        try
        {
            // 1. 解码患者条形码
            var patientBarcode = _barcodeService.RecognizeBarcode(patientBarcodeImage);
            if (patientBarcode == null || patientBarcode.TableName != "Patients")
            {
                _logger.LogWarning("无效的患者条形码");
                return null;
            }
            
            var patientId = patientBarcode.RecordId;

            // 2. 查询该患者所有待执行的护理记录任务
            var tasks = await _executionTaskRepository.ListAsync(t => 
                t.PatientId == patientId &&
                t.Category == TaskCategory.DataCollection &&
                t.Status == ExecutionTaskStatus.Pending);
                
            if (!tasks.Any())
            {
                return null;
            }

            // 3. 找到时间最近的一个 (绝对值最小)
            // 优先找未来的任务，如果没有未来的任务，再找过去最近的未完成任务
            var now = DateTime.UtcNow;
            var nearestTask = tasks
                .OrderBy(t => Math.Abs((t.PlannedStartTime - now).Ticks))
                .FirstOrDefault();
                
            return nearestTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查找最近护理任务失败");
            return null;
        }
    }

    // 辅助类：用于反序列化 DataPayload
    private class TaskPayloadDto
    {
        public string TaskType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsChecklist { get; set; }
        public List<TaskItemDto> Items { get; set; } = new();
        public object? MedicationInfo { get; set; }
    }

    private class TaskItemDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsChecked { get; set; }
        public bool Required { get; set; }
        public string? DrugId { get; set; } // 可能为空
    }

    /// <summary>
    /// 执行所有验证逻辑
    /// </summary>
    private async Task PerformValidationAsync(
        ExecutionTask executionTask, 
        Patient patient, 
        BarcodeMatchingResult result, 
        int toleranceMinutes,
        bool checkTime)
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

        if (checkTime)
        {
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
        }
        else
        {
            result.ValidationDetails.ExecutionTimeMatched = true;
            _logger.LogDebug("跳过执行时间检查");
        }

        // 验证3: 执行任务状态是否允许执行
        var allowedStatuses = new[] {ExecutionTaskStatus.Pending, ExecutionTaskStatus.InProgress};
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
}