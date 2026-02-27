using CareFlow.Application.DTOs.OrderAcknowledgement;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 医嘱签收控制器
/// 提供护士签收医嘱、退回医嘱、查询待签收医嘱等功能
/// </summary>
[ApiController]
[Route("api/orders/acknowledgement")]
public class OrderAcknowledgementController : ControllerBase
{
    private readonly IOrderAcknowledgementService _acknowledgementService;
    private readonly ILogger<OrderAcknowledgementController> _logger;

    public OrderAcknowledgementController(
        IOrderAcknowledgementService acknowledgementService,
        ILogger<OrderAcknowledgementController> logger)
    {
        _acknowledgementService = acknowledgementService;
        _logger = logger;
    }

    /// <summary>
    /// 获取科室所有患者的未签收医嘱统计（用于红点标注）
    /// </summary>
    /// <param name="deptCode">科室代码</param>
    /// <returns>患者列表及其未签收医嘱数量</returns>
    [HttpGet("pending-summary")]
    public async Task<ActionResult<List<PatientUnacknowledgedSummaryDto>>> GetPendingOrdersSummary(
        [FromQuery] string deptCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deptCode))
            {
                return BadRequest(new { message = "科室代码不能为空" });
            }

            _logger.LogInformation("查询科室 {DeptCode} 的患者未签收医嘱统计", deptCode);

            var result = await _acknowledgementService.GetPendingOrdersSummaryAsync(deptCode);

            _logger.LogInformation("✅ 成功返回 {Count} 个患者的统计数据", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取患者未签收医嘱统计失败");
            return StatusCode(500, new { message = "获取数据失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定患者的待签收医嘱列表（包括新开和停止）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>患者的新开医嘱和停止医嘱列表</returns>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<PatientPendingOrdersDto>> GetPatientPendingOrders(string patientId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return BadRequest(new { message = "患者ID不能为空" });
            }

            _logger.LogInformation("查询患者 {PatientId} 的待签收医嘱", patientId);

            var result = await _acknowledgementService.GetPatientPendingOrdersAsync(patientId);

            _logger.LogInformation("✅ 新开医嘱: {NewCount}, 停止医嘱: {StoppedCount}",
                result.NewOrders.Count, result.StoppedOrders.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取患者待签收医嘱失败");
            return StatusCode(500, new { message = "获取数据失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量签收医嘱（核心功能）
    /// 签收后会自动生成执行任务并分配责任护士
    /// </summary>
    /// <param name="request">签收请求（包含护士ID和医嘱ID列表）</param>
    /// <returns>签收结果，包含每条医嘱的任务生成情况</returns>
    [HttpPost("acknowledge")]
    public async Task<ActionResult<AcknowledgeOrdersResponseDto>> AcknowledgeOrders(
        [FromBody] AcknowledgeOrdersRequestDto request)
    {
        try
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(request.NurseId))
            {
                return BadRequest(new { message = "护士ID不能为空" });
            }

            if (request.OrderIds == null || request.OrderIds.Count == 0)
            {
                return BadRequest(new { message = "医嘱ID列表不能为空" });
            }

            _logger.LogInformation("接收到签收请求: 护士 {NurseId}, 医嘱数量 {Count}",
                request.NurseId, request.OrderIds.Count);

            var result = await _acknowledgementService.AcknowledgeOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 签收成功: {SuccessCount} 条", result.Results.Count);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 签收部分失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量签收医嘱时发生异常");
            return StatusCode(500, new AcknowledgeOrdersResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 批量退回医嘱
    /// 将医嘱状态改为Draft，让医生重新修改
    /// </summary>
    /// <param name="request">退回请求（包含护士ID、医嘱ID列表和退回原因）</param>
    /// <returns>退回结果</returns>
    [HttpPost("reject")]
    public async Task<ActionResult<RejectOrdersResponseDto>> RejectOrders(
        [FromBody] RejectOrdersRequestDto request)
    {
        try
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(request.NurseId))
            {
                return BadRequest(new { message = "护士ID不能为空" });
            }

            if (request.OrderIds == null || request.OrderIds.Count == 0)
            {
                return BadRequest(new { message = "医嘱ID列表不能为空" });
            }

            if (string.IsNullOrWhiteSpace(request.RejectReason))
            {
                return BadRequest(new { message = "退回原因不能为空" });
            }

            _logger.LogInformation("接收到退回请求: 护士 {NurseId}, 医嘱数量 {Count}, 原因: {Reason}",
                request.NurseId, request.OrderIds.Count, request.RejectReason);

            var result = await _acknowledgementService.RejectOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 退回成功: {Count} 条", result.RejectedOrderIds.Count);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 退回部分失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量退回医嘱时发生异常");
            return StatusCode(500, new RejectOrdersResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 拒绝停止医嘱
    /// 护士认为不应该停止该医嘱，将医嘱状态从PendingStop恢复为InProgress
    /// </summary>
    /// <param name="request">拒绝停嘱请求（包含护士ID、医嘱ID列表和拒绝原因）</param>
    /// <returns>拒绝停嘱结果</returns>
    [HttpPost("reject-stop")]
    public async Task<ActionResult<RejectStopOrderResponseDto>> RejectStopOrder(
        [FromBody] RejectStopOrderRequestDto request)
    {
        try
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(request.NurseId))
            {
                return BadRequest(new { message = "护士ID不能为空" });
            }

            if (request.OrderIds == null || request.OrderIds.Count == 0)
            {
                return BadRequest(new { message = "医嘱ID列表不能为空" });
            }

            if (string.IsNullOrWhiteSpace(request.RejectReason))
            {
                return BadRequest(new { message = "拒绝原因不能为空" });
            }

            _logger.LogInformation("接收到拒绝停嘱请求: 护士 {NurseId}, 医嘱数量 {Count}, 原因: {Reason}",
                request.NurseId, request.OrderIds.Count, request.RejectReason);

            var result = await _acknowledgementService.RejectStopOrderAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 拒绝停嘱成功: {Count} 条，恢复任务 {TaskCount} 个",
                    result.RejectedOrderIds.Count, result.RestoredTaskIds.Count);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 拒绝停嘱部分失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 拒绝停嘱时发生异常");
            return StatusCode(500, new RejectStopOrderResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // ==================== TODO: 阶段三实现以下接口 ====================

    /// <summary>
    /// TODO: 阶段三 - 立即申请药品（用于今日需执行的药品医嘱）
    /// </summary>
    /// <remarks>
    /// 功能说明：
    /// 1. 签收药品医嘱后，如果今天有取药任务，护士可以立即向药房申请药品
    /// 2. 需要对接药房系统（如果有）或更新任务状态标记为"已申请"
    /// 3. 防止重复申请
    /// </remarks>
    [HttpPost("request-medication-immediately")]
    public async Task<ActionResult> RequestMedicationImmediately(
        [FromBody] object request)
    {
        // TODO: 实现立即申请药品逻辑
        return StatusCode(501, new { message = "功能开发中，待阶段三实现" });
    }

    /// <summary>
    /// TODO: 阶段三 - 申请检查（用于检查医嘱）
    /// </summary>
    /// <remarks>
    /// 功能说明：
    /// 1. 签收检查医嘱后，护士可以立即向检查站申请预约
    /// 2. 需要对接检查站系统（PACS/RIS/LIS）
    /// 3. 返回预约时间和地点
    /// </remarks>
    [HttpPost("request-inspection")]
    public async Task<ActionResult> RequestInspection(
        [FromBody] object request)
    {
        // TODO: 实现申请检查逻辑
        return StatusCode(501, new { message = "功能开发中，待阶段三实现" });
    }

    /// <summary>
    /// TODO: 阶段三 - 取消药品申请（用于停止医嘱后撤回已申请的药品）
    /// </summary>
    /// <remarks>
    /// 功能说明：
    /// 1. 签收停止医嘱时，如果有已申请但未执行的药品，需要取消申请
    /// 2. 通知药房取消配药
    /// 3. 更新任务状态
    /// </remarks>
    [HttpPost("cancel-medication-request")]
    public async Task<ActionResult> CancelMedicationRequest(
        [FromBody] object request)
    {
        // TODO: 实现取消药品申请逻辑
        return StatusCode(501, new { message = "功能开发中，待阶段三实现" });
    }
}
