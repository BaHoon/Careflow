using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CareFlow.Application.Services.OrderApplication;

/// <summary>
/// 药房系统集成服务
/// 模拟与外部药房系统的交互
/// </summary>
public class PharmacyIntegrationService : IPharmacyIntegrationService
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<PharmacyIntegrationService> _logger;

    public PharmacyIntegrationService(
        IRepository<ExecutionTask, long> taskRepository,
        IBackgroundJobService backgroundJobService,
        ILogger<PharmacyIntegrationService> logger)
    {
        _taskRepository = taskRepository;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// 发送取药请求到药房系统
    /// </summary>
    public async Task<PharmacyRequestResult> SendMedicationRequestAsync(
        List<long> taskIds, bool isUrgent)
    {
        _logger.LogInformation("========== 发送取药请求到药房系统 ==========");
        _logger.LogInformation("📤 任务数量: {Count}, 加急: {IsUrgent}", 
            taskIds.Count, isUrgent);

        try
        {
            // TODO: 对接真实药房系统API
            // 示例代码:
            // var httpClient = _httpClientFactory.CreateClient("PharmacySystem");
            // var response = await httpClient.PostAsJsonAsync("/api/medication/requests", new
            // {
            //     TaskIds = taskIds,
            //     IsUrgent = isUrgent,
            //     RequestTime = DateTime.UtcNow
            // });
            // var result = await response.Content.ReadFromJsonAsync<PharmacyResponse>();

            // 模拟：药房系统立即接受请求
            var result = new PharmacyRequestResult
            {
                Success = true,
                Message = "药房已接受取药请求（模拟）",
                AcceptedTaskIds = taskIds,
                EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(isUrgent ? 1 : 3)
            };

            _logger.LogInformation("✅ 药房接受请求成功，预计完成时间: {Time}", 
                result.EstimatedCompletionTime);

            // 启动后台任务：延迟N分钟后自动确认配药完成
            var delayMinutes = isUrgent ? 1 : 3;
            _backgroundJobService.ScheduleDelayed(
                () => ConfirmPharmacyCompletionAsync(taskIds),
                TimeSpan.FromMinutes(delayMinutes)
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送取药请求失败");
            return new PharmacyRequestResult
            {
                Success = false,
                Message = $"发送取药请求失败: {ex.Message}",
                AcceptedTaskIds = new List<long>()
            };
        }
    }

    /// <summary>
    /// 取消取药请求
    /// </summary>
    public async Task<bool> CancelMedicationRequestAsync(List<long> taskIds)
    {
        _logger.LogInformation("========== 撤销取药请求 ==========");
        _logger.LogInformation("❌ 任务数量: {Count}", taskIds.Count);

        try
        {
            // TODO: 调用药房系统撤销API
            // var httpClient = _httpClientFactory.CreateClient("PharmacySystem");
            // var response = await httpClient.PostAsJsonAsync("/api/medication/cancel", new
            // {
            //     TaskIds = taskIds,
            //     CancelTime = DateTime.UtcNow
            // });

            // 模拟：直接返回成功
            _logger.LogInformation("✅ 药房接受撤销请求（模拟）");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销取药请求失败");
            return false;
        }
    }

    /// <summary>
    /// 确认药房配药完成（后台任务调用）
    /// </summary>
    private async Task ConfirmPharmacyCompletionAsync(List<long> taskIds)
    {
        _logger.LogInformation("========== 药房配药完成确认 ==========");
        _logger.LogInformation("💊 确认任务数量: {Count}", taskIds.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var taskId in taskIds)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                
                if (task == null)
                {
                    _logger.LogWarning("⚠️ 任务 {TaskId} 不存在", taskId);
                    failCount++;
                    continue;
                }

                // 只有Applied状态的任务才能确认
                if (task.Status != ExecutionTaskStatus.Applied)
                {
                    _logger.LogWarning("⚠️ 任务 {TaskId} 状态为 {Status}，跳过确认", 
                        taskId, task.Status);
                    failCount++;
                    continue;
                }

                // 更新任务状态为AppliedConfirmed
                task.Status = ExecutionTaskStatus.AppliedConfirmed;
                task.LastModifiedAt = DateTime.UtcNow;

                // 更新DataPayload，添加确认信息
                try
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        task.DataPayload);
                    
                    if (payload != null)
                    {
                        payload["PharmacyConfirmedAt"] = JsonSerializer.SerializeToElement(DateTime.UtcNow);
                        payload["PharmacyConfirmedMessage"] = JsonSerializer.SerializeToElement("配药完成");
                        
                        task.DataPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                    }
                }
                catch (Exception payloadEx)
                {
                    _logger.LogWarning(payloadEx, "⚠️ 更新DataPayload失败，任务ID: {TaskId}", taskId);
                    // 即使Payload更新失败，状态更新仍然有效，继续执行
                }

                await _taskRepository.UpdateAsync(task);
                successCount++;
                
                _logger.LogInformation("✅ 任务 {TaskId} 确认完成", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 确认任务 {TaskId} 失败", taskId);
                failCount++;
            }
        }

        _logger.LogInformation("========== 药房配药确认完成：成功 {Success}，失败 {Fail} ==========",
            successCount, failCount);
    }
}
