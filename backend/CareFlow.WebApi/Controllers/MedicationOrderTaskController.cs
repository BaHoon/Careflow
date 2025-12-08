using CareFlow.Application.DTOs.MedicationOrder;
using CareFlow.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicationOrderTaskController : ControllerBase
{
    private readonly IMedicationOrderTaskService _taskService;
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicationOrder, long> _orderRepository;
    private readonly ILogger<MedicationOrderTaskController> _logger;

    public MedicationOrderTaskController(
        IMedicationOrderTaskService taskService,
        IRepository<CareFlow.Core.Models.Medical.MedicationOrder, long> orderRepository,
        ILogger<MedicationOrderTaskController> logger)
    {
        _taskService = taskService;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    /// <summary>
    /// 为指定的medication order生成执行任务
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateTasks([FromBody] GenerateTasksRequestDto request)
    {
        try
        {
            _logger.LogInformation("开始为医嘱 {OrderId} 生成执行任务", request.MedicationOrderId);

            // 1. 获取医嘱信息
            var order = await _orderRepository.GetByIdAsync(request.MedicationOrderId);
            if (order == null)
            {
                return NotFound(new GenerateTasksResponseDto
                {
                    Success = false,
                    Message = $"未找到ID为 {request.MedicationOrderId} 的医嘱"
                });
            }

            // 2. 生成执行任务
            var tasks = await _taskService.GenerateExecutionTasksAsync(order);

            // 3. 转换为DTO
            var taskDtos = tasks.Select(t => new ExecutionTaskDto
            {
                Id = t.Id,
                MedicalOrderId = t.MedicalOrderId,
                PatientId = t.PatientId,
                PlannedStartTime = t.PlannedStartTime,
                ActualStartTime = t.ActualStartTime,
                ActualEndTime = t.ActualEndTime,
                ExecutorStaffId = t.ExecutorStaffId,
                Status = t.Status,
                IsRolledBack = t.IsRolledBack,
                DataPayload = t.DataPayload,
                ExceptionReason = t.ExceptionReason,
                CreatedAt = t.CreatedAt
            }).ToList();

            var response = new GenerateTasksResponseDto
            {
                Success = true,
                Message = $"成功为医嘱生成 {tasks.Count} 个执行任务",
                Tasks = taskDtos,
                TaskCount = tasks.Count
            };

            _logger.LogInformation("成功为医嘱 {OrderId} 生成了 {TaskCount} 个执行任务", request.MedicationOrderId, tasks.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为医嘱 {OrderId} 生成执行任务时发生错误", request.MedicationOrderId);
            return BadRequest(new GenerateTasksResponseDto
            {
                Success = false,
                Message = $"生成任务失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 回滚指定医嘱的未执行任务
    /// </summary>
    [HttpPost("{orderId}/rollback")]
    public async Task<IActionResult> RollbackTasks(long orderId, [FromBody] string reason)
    {
        try
        {
            _logger.LogInformation("开始回滚医嘱 {OrderId} 的未执行任务", orderId);

            await _taskService.RollbackPendingTasksAsync(orderId, reason ?? "手动回滚");

            _logger.LogInformation("成功回滚医嘱 {OrderId} 的未执行任务", orderId);
            return Ok(new { Success = true, Message = "任务回滚成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚医嘱 {OrderId} 的任务时发生错误", orderId);
            return BadRequest(new { Success = false, Message = $"回滚失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 刷新指定医嘱的执行任务（重新生成）
    /// </summary>
    [HttpPost("{orderId}/refresh")]
    public async Task<IActionResult> RefreshTasks(long orderId)
    {
        try
        {
            _logger.LogInformation("开始刷新医嘱 {OrderId} 的执行任务", orderId);

            // 1. 获取医嘱信息
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { Success = false, Message = $"未找到ID为 {orderId} 的医嘱" });
            }

            // 2. 刷新任务
            await _taskService.RefreshExecutionTasksAsync(order);

            _logger.LogInformation("成功刷新医嘱 {OrderId} 的执行任务", orderId);
            return Ok(new { Success = true, Message = "任务刷新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新医嘱 {OrderId} 的任务时发生错误", orderId);
            return BadRequest(new { Success = false, Message = $"刷新失败: {ex.Message}" });
        }
    }
}