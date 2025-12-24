using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CareFlow.WebApi.Controller;

/// <summary>
/// 医嘱申请控制器
/// </summary>
[ApiController]
[Route("api/orders/application")]
public class OrderApplicationController : ControllerBase
{
    private readonly IOrderApplicationService _applicationService;
    private readonly ILogger<OrderApplicationController> _logger;

    public OrderApplicationController(
        IOrderApplicationService applicationService,
        ILogger<OrderApplicationController> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取药品申请列表
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>申请项列表</returns>
    [HttpPost("medication/list")]
    [ProducesResponseType(typeof(List<ApplicationItemDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<ApplicationItemDto>>> GetMedicationApplications(
        [FromBody] GetApplicationListRequestDto request)
    {
        try
        {
            _logger.LogInformation("📋 获取药品申请列表，患者数: {Count}", request.PatientIds.Count);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.GetMedicationApplicationsAsync(request);
            
            _logger.LogInformation("✅ 返回 {Count} 条药品申请记录", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取药品申请列表失败");
            return StatusCode(500, new { message = "获取药品申请列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取检查申请列表
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>申请项列表</returns>
    [HttpPost("inspection/list")]
    [ProducesResponseType(typeof(List<ApplicationItemDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<ApplicationItemDto>>> GetInspectionApplications(
        [FromBody] GetApplicationListRequestDto request)
    {
        try
        {
            _logger.LogInformation("📋 获取检查申请列表，患者数: {Count}", request.PatientIds.Count);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.GetInspectionApplicationsAsync(request);
            
            _logger.LogInformation("✅ 返回 {Count} 条检查申请记录", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取检查申请列表失败");
            return StatusCode(500, new { message = "获取检查申请列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 提交药品申请
    /// </summary>
    /// <param name="request">申请请求</param>
    /// <returns>申请结果</returns>
    [HttpPost("medication/submit")]
    [ProducesResponseType(typeof(ApplicationResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApplicationResponseDto>> SubmitMedicationApplication(
        [FromBody] MedicationApplicationRequestDto request)
    {
        try
        {
            _logger.LogInformation("💊 提交药品申请，护士: {NurseId}, 任务数: {Count}, 加急: {IsUrgent}",
                request.NurseId, request.TaskIds.Count, request.IsUrgent);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.SubmitMedicationApplicationAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ 药品申请提交成功");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 药品申请部分失败: {Message}", result.Message);
                return Ok(result); // 返回200但包含错误信息
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 提交药品申请失败");
            return StatusCode(500, new { message = "提交药品申请失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 提交检查申请
    /// </summary>
    /// <param name="request">申请请求</param>
    /// <returns>申请结果</returns>
    [HttpPost("inspection/submit")]
    [ProducesResponseType(typeof(ApplicationResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApplicationResponseDto>> SubmitInspectionApplication(
        [FromBody] InspectionApplicationRequestDto request)
    {
        try
        {
            _logger.LogInformation("🔬 提交检查申请，护士: {NurseId}, 任务数: {Count}, 加急: {IsUrgent}",
                request.NurseId, request.TaskIds.Count, request.IsUrgent);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.SubmitInspectionApplicationAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ 检查申请提交成功");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 检查申请部分失败: {Message}", result.Message);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 提交检查申请失败");
            return StatusCode(500, new { message = "提交检查申请失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 撤销药品申请
    /// </summary>
    /// <param name="request">撤销请求</param>
    /// <returns>撤销结果</returns>
    [HttpPost("medication/cancel")]
    [ProducesResponseType(typeof(ApplicationResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApplicationResponseDto>> CancelMedicationApplication(
        [FromBody] CancelApplicationRequestDto request)
    {
        try
        {
            _logger.LogInformation("❌ 撤销药品申请，护士: {NurseId}, 任务数: {Count}",
                request.NurseId, request.Ids.Count);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.CancelMedicationApplicationAsync(
                request.Ids, request.NurseId, request.Reason);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ 药品申请撤销成功");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 药品申请撤销部分失败: {Message}", result.Message);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销药品申请失败");
            return StatusCode(500, new { message = "撤销药品申请失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 申请退药（已确认状态的药品）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="request">退药请求</param>
    /// <returns>退药结果</returns>
    [HttpPost("medication/return/{taskId}")]
    [ProducesResponseType(typeof(ApplicationResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApplicationResponseDto>> RequestReturnMedication(
        long taskId,
        [FromBody] ReturnMedicationRequestDto request)
    {
        try
        {
            _logger.LogInformation("🔙 申请退药，任务: {TaskId}, 护士: {NurseId}", taskId, request.NurseId);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.RequestReturnMedicationAsync(
                taskId, request.NurseId, request.Reason);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ 退药申请成功");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 退药申请失败: {Message}", result.Message);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 申请退药失败");
            return StatusCode(500, new { message = "申请退药失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 确认退药（待退药状态的药品）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="request">确认请求</param>
    /// <returns>确认结果</returns>
    [HttpPost("medication/return/{taskId}/confirm")]
    [ProducesResponseType(typeof(ApplicationResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApplicationResponseDto>> ConfirmReturnMedication(
        long taskId,
        [FromBody] ConfirmReturnRequestDto request)
    {
        try
        {
            _logger.LogInformation("✅ 确认退药，任务: {TaskId}, 护士: {NurseId}", taskId, request.NurseId);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.ConfirmReturnMedicationAsync(
                taskId, request.NurseId);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ 退药确认成功");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 退药确认失败: {Message}", result.Message);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认退药失败");
            return StatusCode(500, new { message = "确认退药失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 撤销检查申请
    /// </summary>
    /// <param name="request">撤销请求</param>
    /// <returns>撤销结果</returns>
    [HttpPost("inspection/cancel")]
    [ProducesResponseType(typeof(ApplicationResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApplicationResponseDto>> CancelInspectionApplication(
        [FromBody] CancelApplicationRequestDto request)
    {
        try
        {
            _logger.LogInformation("❌ 撤销检查申请，护士: {NurseId}, 医嘱数: {Count}",
                request.NurseId, request.Ids.Count);
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.CancelInspectionApplicationAsync(
                request.Ids, request.NurseId, request.Reason);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ 检查申请撤销成功");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 检查申请撤销部分失败: {Message}", result.Message);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销检查申请失败");
            return StatusCode(500, new { message = "撤销检查申请失败", error = ex.Message });
        }
    }
}
