using CareFlow.Application.DTOs.Common;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 医生端医嘱查询控制器
/// 提供医嘱查询、详情查看、停止医嘱等功能
/// </summary>
[ApiController]
[Route("api/doctor/orders")]
public class DoctorOrderController : ControllerBase
{
    private readonly IMedicalOrderQueryService _queryService;
    private readonly ILogger<DoctorOrderController> _logger;

    public DoctorOrderController(
        IMedicalOrderQueryService queryService,
        ILogger<DoctorOrderController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// 查询患者医嘱列表（支持多条件筛选）
    /// </summary>
    /// <param name="request">查询条件</param>
    /// <returns>医嘱列表和统计信息</returns>
    /// <remarks>
    /// 筛选条件说明：
    /// - PatientId: 必填，患者ID
    /// - Statuses: 可选，医嘱状态列表（可多选）
    /// - OrderTypes: 可选，医嘱类型列表（可多选）
    /// - CreateTimeFrom/CreateTimeTo: 可选，创建时间范围
    /// - SortBy: 排序字段（CreateTime, Status, OrderType）
    /// - SortDescending: 是否降序（默认true）
    /// 
    /// 示例请求：
    /// {
    ///   "patientId": "P001",
    ///   "statuses": [1, 2, 3],  // PendingReceive, Accepted, InProgress
    ///   "orderTypes": ["MedicationOrder", "SurgicalOrder"],
    ///   "createTimeFrom": "2025-12-01T00:00:00Z",
    ///   "createTimeTo": "2025-12-23T23:59:59Z"
    /// }
    /// </remarks>
    [HttpPost("query")]
    public async Task<ActionResult<QueryOrdersResponseDto>> QueryOrders(
        [FromBody] QueryOrdersRequestDto request)
    {
        try
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(request.PatientId))
            {
                return BadRequest(new { message = "患者ID不能为空" });
            }

            _logger.LogInformation("查询患者 {PatientId} 的医嘱列表", request.PatientId);

            var result = await _queryService.GetOrdersByPatientAsync(request);

            _logger.LogInformation("✅ 查询成功，返回 {Count} 条医嘱", result.TotalCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询医嘱列表失败");
            return StatusCode(500, new { message = "查询失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取医嘱详细信息（包含关联任务列表）
    /// </summary>
    /// <param name="orderId">医嘱ID</param>
    /// <returns>医嘱完整信息</returns>
    /// <remarks>
    /// 返回内容包括：
    /// - 医嘱基础信息（状态、类型、时间等）
    /// - 根据类型返回特定字段：
    ///   * MedicationOrder: 药品列表、用药途径、时间策略等
    ///   * SurgicalOrder: 手术名称、麻醉方式、术前准备等
    ///   * InspectionOrder: 检查项目代码和名称
    ///   * OperationOrder: 操作名称和部位
    /// - 关联的所有执行任务及状态
    /// - 审计信息（签收、停嘱记录等）
    /// </remarks>
    [HttpGet("{orderId}/detail")]
    public async Task<ActionResult<OrderDetailDto>> GetOrderDetail(long orderId)
    {
        try
        {
            if (orderId <= 0)
            {
                return BadRequest(new { message = "医嘱ID无效" });
            }

            _logger.LogInformation("获取医嘱 {OrderId} 的详细信息", orderId);

            var result = await _queryService.GetOrderDetailAsync(orderId);

            _logger.LogInformation("✅ 获取成功，医嘱类型: {OrderType}, 包含 {TaskCount} 个任务", 
                result.OrderType, result.Tasks.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取医嘱详情失败: {OrderId}", orderId);
            
            if (ex.Message.Contains("不存在"))
            {
                return NotFound(new { message = ex.Message });
            }

            return StatusCode(500, new { message = "获取详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 停止医嘱（医生下达停嘱指令）
    /// </summary>
    /// <param name="request">停嘱请求</param>
    /// <returns>停嘱结果，包含被锁定的任务列表</returns>
    /// <remarks>
    /// 业务逻辑：
    /// 1. 验证医嘱状态：
    ///    - PendingReceive（未签收）：直接取消，状态改为 Cancelled，无需护士签收
    ///    - Accepted 或 InProgress（已签收）：改为 PendingStop，需要护士签收
    /// 2. 对于未签收医嘱：
    ///    - 医嘱状态直接改为 Cancelled
    ///    - 无需锁定任务（因为还没有任务生成）
    /// 3. 对于已签收医嘱：
    ///    - 医嘱状态改为 PendingStop
    ///    - 锁定 StopAfterTaskId 之后的所有未完成任务：
    ///      * 保存任务原状态到 StatusBeforeLocking
    ///      * 任务状态改为 OrderStopping
    /// 4. 护士后续操作（仅针对已签收医嘱）：
    ///    - 签收停嘱 → 医嘱变为 Stopped，任务变为 Stopped
    ///    - 拒绝停嘱 → 医嘱恢复 InProgress，任务恢复原状态
    /// 
    /// 示例请求：
    /// {
    ///   "orderId": 123,
    ///   "doctorId": "D001",
    ///   "stopReason": "患者病情好转，无需继续用药",
    ///   "stopAfterTaskId": 456  // 从这个任务之后停止（未签收医嘱可填0）
    /// }
    /// 
    /// 状态码说明：
    /// - 200: 停嘱成功
    /// - 400: 参数错误或状态不允许停止
    /// - 500: 服务器内部错误
    /// </remarks>
    [HttpPost("stop")]
    public async Task<ActionResult<StopOrderResponseDto>> StopOrder(
        [FromBody] StopOrderRequestDto request)
    {
        try
        {
            // 参数验证
            if (request.OrderId <= 0)
            {
                return BadRequest(new { message = "医嘱ID无效" });
            }

            if (string.IsNullOrWhiteSpace(request.DoctorId))
            {
                return BadRequest(new { message = "医生ID不能为空" });
            }

            if (string.IsNullOrWhiteSpace(request.StopReason))
            {
                return BadRequest(new { message = "停嘱原因不能为空" });
            }

            // 注意：未签收医嘱（PendingReceive）可以不指定停止节点（stopAfterTaskId可以为0或null）
            // 已签收医嘱必须指定停止节点，但这个验证在Service层进行

            _logger.LogInformation("接收停嘱请求: 医嘱 {OrderId}, 医生 {DoctorId}, 停止节点 {StopAfterTaskId}",
                request.OrderId, request.DoctorId, request.StopAfterTaskId);

            var result = await _queryService.StopOrderAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 停嘱成功: 医嘱 {OrderId}, 锁定 {LockedCount} 个任务",
                    request.OrderId, result.LockedTaskIds.Count);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 停嘱失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 停止医嘱时发生异常: {OrderId}", request.OrderId);
            return StatusCode(500, new StopOrderResponseDto
            {
                Success = false,
                OrderId = request.OrderId,
                Message = "停嘱失败",
                Errors = new List<string> { $"系统错误: {ex.Message}" }
            });
        }
    }

    /// <summary>
    /// 重新提交已退回的医嘱
    /// </summary>
    /// <param name="request">重新提交请求</param>
    /// <returns>操作结果</returns>
    /// <remarks>
    /// 业务逻辑：
    /// 1. 验证医嘱状态（只有 Rejected 可重新提交）
    /// 2. 验证操作人（只有开单医生才能重新提交）
    /// 3. 医嘱状态改为 PendingReceive（重新进入护士待签收列表）
    /// 4. 记录状态变更历史
    /// </remarks>
    [HttpPost("{orderId}/resubmit")]
    public async Task<ActionResult> ResubmitRejectedOrder(
        long orderId,
        [FromBody] ResubmitOrderRequest request)
    {
        try
        {
            if (orderId <= 0)
            {
                return BadRequest(new { message = "医嘱ID无效" });
            }

            if (string.IsNullOrWhiteSpace(request.DoctorId))
            {
                return BadRequest(new { message = "医生ID不能为空" });
            }

            _logger.LogInformation("接收重新提交医嘱请求: 医嘱 {OrderId}, 医生 {DoctorId}",
                orderId, request.DoctorId);

            var result = await _queryService.ResubmitRejectedOrderAsync(orderId, request.DoctorId);

            _logger.LogInformation("✅ 重新提交成功: 医嘱 {OrderId}", orderId);
            return Ok(new { success = true, message = "重新提交成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 重新提交医嘱失败: {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 撤销已退回的医嘱
    /// </summary>
    /// <param name="request">撤销请求</param>
    /// <returns>操作结果</returns>
    /// <remarks>
    /// 业务逻辑：
    /// 1. 验证医嘱状态（只有 Rejected 可撤销）
    /// 2. 验证操作人（只有开单医生才能撤销）
    /// 3. 医嘱状态改为 Cancelled（不可恢复）
    /// 4. 记录撤销原因和状态变更历史
    /// </remarks>
    [HttpPost("{orderId}/cancel")]
    public async Task<ActionResult> CancelRejectedOrder(
        long orderId,
        [FromBody] CancelOrderRequest request)
    {
        try
        {
            if (orderId <= 0)
            {
                return BadRequest(new { message = "医嘱ID无效" });
            }

            if (string.IsNullOrWhiteSpace(request.DoctorId))
            {
                return BadRequest(new { message = "医生ID不能为空" });
            }

            if (string.IsNullOrWhiteSpace(request.CancelReason))
            {
                return BadRequest(new { message = "撤销原因不能为空" });
            }

            _logger.LogInformation("接收撤销医嘱请求: 医嘱 {OrderId}, 医生 {DoctorId}, 原因: {Reason}",
                orderId, request.DoctorId, request.CancelReason);

            var result = await _queryService.CancelRejectedOrderAsync(
                orderId, request.DoctorId, request.CancelReason);

            _logger.LogInformation("✅ 撤销成功: 医嘱 {OrderId}", orderId);
            return Ok(new { success = true, message = "撤销成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 撤销医嘱失败: {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

// DTO类定义
public class ResubmitOrderRequest
{
    public string DoctorId { get; set; } = string.Empty;
}

public class CancelOrderRequest
{
    public string DoctorId { get; set; } = string.Empty;
    public string CancelReason { get; set; } = string.Empty;
}
