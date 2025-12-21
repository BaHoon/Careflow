using CareFlow.Application.Services;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using Microsoft.AspNetCore.Mvc;
using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.OperationOrder;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationOrderTaskController : ControllerBase
{
    private readonly IOperationOrderTaskService _taskService;
    private readonly IRepository<OperationOrder, long> _orderRepository;
    private readonly IRepository<ExecutionTask, long> _executionTaskRepository;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<OperationOrderTaskController> _logger;

    public OperationOrderTaskController(
        IOperationOrderTaskService taskService,
        IRepository<OperationOrder, long> orderRepository,
        IRepository<ExecutionTask, long> executionTaskRepository,
        IBarcodeService barcodeService,
        ILogger<OperationOrderTaskController> logger)
    {
        _taskService = taskService;
        _orderRepository = orderRepository;
        _executionTaskRepository = executionTaskRepository;
        _barcodeService = barcodeService;
        _logger = logger;
    }

    /// <summary>
    /// 为指定的操作医嘱生成执行任务
    /// </summary>
    /// <param name="orderId">操作医嘱ID</param>
    [HttpPost("{orderId}/generate")]
    public async Task<IActionResult> GenerateTasks(long orderId)
    {
        try
        {
            _logger.LogInformation("开始为操作医嘱 {OrderId} 生成执行任务", orderId);

            // 直接通过医嘱ID调用Service生成任务
            // Service内部会：查询医嘱表 -> 根据逻辑拆分任务 -> 保存到任务表
            var tasks = await _taskService.GenerateExecutionTasksAsync(orderId);

            // 3. 转换为通用 DTO
            var taskDtos = MapToDtos(tasks);

            var response = new TaskGenerationResultDto
            {
                Success = true,
                Message = $"成功生成 {tasks.Count} 个操作执行任务",
                Tasks = taskDtos,
                TaskCount = tasks.Count
            };

            _logger.LogInformation("生成完成，共 {Count} 个任务", tasks.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成任务失败，医嘱ID: {OrderId}", orderId);
            return BadRequest(new TaskGenerationResultDto
            {
                Success = false,
                Message = $"系统异常: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 回滚(取消)指定操作医嘱的未执行任务
    /// </summary>
    [HttpPost("{orderId}/rollback")]
    public async Task<IActionResult> RollbackTasks(long orderId, [FromBody] string reason)
    {
        try
        {
            _logger.LogInformation("请求回滚操作医嘱 {OrderId} 的任务", orderId);
            
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound("医嘱不存在");

            await _taskService.RollbackPendingTasksAsync(orderId, reason ?? "用户手动回滚");

            return Ok(new { Success = true, Message = "任务回滚成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚失败");
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// 刷新任务 (当操作医嘱变更后调用)
    /// </summary>
    [HttpPost("{orderId}/refresh")]
    public async Task<IActionResult> RefreshTasks(long orderId)
    {
        try
        {
            _logger.LogInformation("请求刷新操作医嘱 {OrderId} 的任务", orderId);

            await _taskService.RefreshExecutionTasksAsync(orderId);

            return Ok(new { Success = true, Message = "任务刷新重置成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新失败");
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// 获取某条执行任务的条形码图片（用于扫码验证患者身份）
    /// </summary>
    [HttpGet("task/{taskId}/barcode")]
    public async Task<IActionResult> GetTaskBarcode(long taskId)
    {
        try
        {
            var task = await _executionTaskRepository.GetByIdAsync(taskId);
            if (task == null) return NotFound("任务不存在");

            var barcodeIndex = new BarcodeIndex
            {
                TableName = "ExecutionTasks",
                RecordId = taskId.ToString()
            };

            var imageBytes = await _barcodeService.GenerateBarcodeAsync(barcodeIndex);
            return File(imageBytes, "image/png", $"ExecTask-{taskId}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取条形码失败: {Message}", ex.Message);
            return BadRequest($"获取条形码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 查询某操作医嘱关联的所有任务列表
    /// </summary>
    [HttpGet("{orderId}/tasks")]
    public async Task<IActionResult> GetOrderTasks(long orderId)
    {
        try
        {
            var tasks = await _executionTaskRepository.ListAsync(t => t.MedicalOrderId == orderId);
            var taskDtos = MapToDtos(tasks);

            return Ok(new 
            { 
                Success = true, 
                OrderId = orderId,
                Tasks = taskDtos 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询任务列表失败: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    // --- 私有辅助方法 ---
    private List<ExecutionTaskDto> MapToDtos(IEnumerable<ExecutionTask> tasks)
    {
        return tasks.Select(t => new ExecutionTaskDto
        {
            Id = t.Id,
            MedicalOrderId = t.MedicalOrderId,
            PatientId = t.PatientId,
            Category = t.Category,
            PlannedStartTime = t.PlannedStartTime,
            ActualStartTime = t.ActualStartTime,
            ActualEndTime = t.ActualEndTime,
            ExecutorStaffId = t.ExecutorStaffId,
            CompleterNurseId = t.CompleterNurseId,
            Status = t.Status,
            IsRolledBack = t.IsRolledBack,
            DataPayload = t.DataPayload,
            ResultPayload = t.ResultPayload,
            ExceptionReason = t.ExceptionReason,
            CreatedAt = t.CreatedAt
        }).OrderBy(t => t.PlannedStartTime).ToList();
    }
}

