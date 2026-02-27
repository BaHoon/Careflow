using CareFlow.Application.DTOs.MedicationOrders;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicationOrderTaskController : ControllerBase
{
    private readonly IMedicationOrderTaskService _taskService;
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicationOrder, long> _orderRepository;
    private readonly IRepository<CareFlow.Core.Models.Nursing.ExecutionTask, long> _executionTaskRepository;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<MedicationOrderTaskController> _logger;

    public MedicationOrderTaskController(
        IMedicationOrderTaskService taskService,
        IRepository<CareFlow.Core.Models.Medical.MedicationOrder, long> orderRepository,
        IRepository<CareFlow.Core.Models.Nursing.ExecutionTask, long> executionTaskRepository,
        IBarcodeService barcodeService,
        ILogger<MedicationOrderTaskController> logger)
    {
        _taskService = taskService;
        _orderRepository = orderRepository;
        _executionTaskRepository = executionTaskRepository;
        _barcodeService = barcodeService;
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

    /// <summary>
    /// 获取ExecutionTask的条形码
    /// </summary>
    [HttpGet("task/{taskId}/barcode")]
    public async Task<IActionResult> GetTaskBarcode(long taskId)
    {
        try
        {
            _logger.LogInformation("开始获取ExecutionTask {TaskId} 的条形码", taskId);

            // 1. 验证任务是否存在
            var task = await _executionTaskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return NotFound(new { Success = false, Message = $"未找到ID为 {taskId} 的执行任务" });
            }

            // 2. 创建条形码索引对象
            var barcodeIndex = new BarcodeIndex
            {
                TableName = "ExecutionTasks",
                RecordId = taskId.ToString()
            };

            // 3. 生成条形码图片
            var imageBytes = await _barcodeService.GenerateBarcodeAsync(barcodeIndex);

            _logger.LogInformation("成功生成ExecutionTask {TaskId} 的条形码", taskId);
            return File(imageBytes, "image/png", $"ExecutionTask-{taskId}-barcode.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取ExecutionTask {TaskId} 的条形码时发生错误", taskId);
            return BadRequest(new { Success = false, Message = $"获取条形码失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 获取指定医嘱的所有ExecutionTask列表（包含条形码信息）
    /// </summary>
    [HttpGet("{orderId}/tasks")]
    public async Task<IActionResult> GetOrderTasks(long orderId)
    {
        try
        {
            _logger.LogInformation("开始获取医嘱 {OrderId} 的所有执行任务", orderId);

            // 1. 验证医嘱是否存在
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { Success = false, Message = $"未找到ID为 {orderId} 的医嘱" });
            }

            // 2. 获取所有关联的执行任务
            var tasks = await _executionTaskRepository.ListAsync(t => t.MedicalOrderId == orderId);

            // 3. 转换为DTO并添加条形码信息
            var taskDtos = tasks.Select(t => new ExecutionTaskDto
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

            var response = new
            {
                Success = true,
                Message = $"找到 {tasks.Count()} 个执行任务",
                OrderId = orderId,
                Tasks = taskDtos,
                TaskCount = tasks.Count()
            };

            _logger.LogInformation("成功获取医嘱 {OrderId} 的 {TaskCount} 个执行任务", orderId, tasks.Count());
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医嘱 {OrderId} 的执行任务时发生错误", orderId);
            return BadRequest(new { Success = false, Message = $"获取任务列表失败: {ex.Message}" });
        }
    }
}