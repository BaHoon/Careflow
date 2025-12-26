using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.DischargeOrders;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 出院医嘱控制器
/// </summary>
[ApiController]
[Route("api/orders/discharge")]
public class DischargeOrderController : ControllerBase
{
    private readonly IDischargeOrderService _dischargeOrderService;
    private readonly ILogger<DischargeOrderController> _logger;

    public DischargeOrderController(
        IDischargeOrderService dischargeOrderService,
        ILogger<DischargeOrderController> logger)
    {
        _dischargeOrderService = dischargeOrderService;
        _logger = logger;
    }

    /// <summary>
    /// 验证患者是否可以创建出院医嘱（前置验证）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>验证结果，包含阻塞医嘱和待停止医嘱信息</returns>
    [HttpGet("validate-creation/{patientId}")]
    public async Task<ActionResult<DischargeOrderValidationResult>> ValidateCreation(string patientId)
    {
        try
        {
            _logger.LogInformation("接收到验证出院医嘱创建请求: PatientId={PatientId}", patientId);

            if (string.IsNullOrWhiteSpace(patientId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "患者ID不能为空"
                });
            }

            var result = await _dischargeOrderService.ValidateDischargeOrderCreationAsync(patientId);

            if (result.CanCreateDischargeOrder)
            {
                _logger.LogInformation("✅ 患者 {PatientId} 可以创建出院医嘱", patientId);
            }
            else
            {
                _logger.LogWarning("⚠️ 患者 {PatientId} 存在阻塞医嘱，无法创建出院医嘱", patientId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 验证出院医嘱创建时发生异常");
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 创建出院医嘱
    /// </summary>
    /// <param name="request">出院医嘱创建请求</param>
    /// <returns>创建结果</returns>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchCreateOrderResponseDto>> CreateDischargeOrder(
        [FromBody] BatchCreateDischargeOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("接收到创建出院医嘱请求: PatientId={PatientId}, DoctorId={DoctorId}, DischargeType={DischargeType}",
                request.PatientId, request.DoctorId, request.Orders[0].DischargeType);

            // 参数验证
            if (string.IsNullOrWhiteSpace(request.PatientId))
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "患者ID不能为空",
                    Errors = new List<string> { "patientId字段不能为空" }
                });
            }

            if (string.IsNullOrWhiteSpace(request.DoctorId))
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "医生ID不能为空",
                    Errors = new List<string> { "doctorId字段不能为空" }
                });
            }

            if (request.Orders == null || request.Orders.Count == 0)
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "出院医嘱信息不能为空",
                    Errors = new List<string> { "orders字段不能为空" }
                });
            }

            // 业务验证
            if (request.Orders[0].DischargeTime == default)
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "出院时间不能为空",
                    Errors = new List<string> { "dischargeTime字段不能为空" }
                });
            }

            if (request.Orders[0].DischargeTime < DateTime.UtcNow)
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "出院时间不能早于当前时间",
                    Errors = new List<string> { "dischargeTime不能是过去时间" }
                });
            }

            if (request.Orders[0].DischargeType == 0)
            {
                return BadRequest(new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "出院类型不能为空",
                    Errors = new List<string> { "dischargeType字段不能为空" }
                });
            }

            // 委托给Service层处理业务逻辑
            var result = await _dischargeOrderService.BatchCreateOrdersAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("✅ 出院医嘱创建成功: OrderId={OrderId}", 
                    result.Data?.OrderIds.FirstOrDefault());
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ 出院医嘱创建失败: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建出院医嘱时发生异常");
            return StatusCode(500, new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 验证患者是否可以签收出院医嘱（前置验证）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>验证结果，包含待停止医嘱信息</returns>
    [HttpGet("validate-acknowledgement/{patientId}")]
    public async Task<ActionResult<DischargeOrderAcknowledgementValidationResult>> ValidateAcknowledgement(
        string patientId)
    {
        try
        {
            _logger.LogInformation("接收到验证出院医嘱签收请求: PatientId={PatientId}", patientId);

            if (string.IsNullOrWhiteSpace(patientId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "患者ID不能为空"
                });
            }

            var result = await _dischargeOrderService.ValidateDischargeOrderAcknowledgementAsync(patientId);

            if (result.CanAcknowledge)
            {
                _logger.LogInformation("✅ 患者 {PatientId} 可以签收出院医嘱", patientId);
            }
            else
            {
                _logger.LogWarning("⚠️ 患者 {PatientId} 存在待停止医嘱，无法签收出院医嘱: {Reason}", 
                    patientId, result.Reason);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 验证出院医嘱签收时发生异常");
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 验证出院医嘱数据（可选，用于前端实时校验）
    /// </summary>
    /// <param name="orderDto">出院医嘱DTO</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate")]
    public ActionResult<object> ValidateOrder([FromBody] DischargeOrderDto orderDto)
    {
        try
        {
            var errors = new List<object>();
            var warnings = new List<object>();

            // 基础验证
            if (orderDto.DischargeTime == default)
            {
                errors.Add(new { field = "dischargeTime", message = "出院时间不能为空" });
            }
            else if (orderDto.DischargeTime < DateTime.UtcNow)
            {
                errors.Add(new { field = "dischargeTime", message = "出院时间不能早于当前时间" });
            }

            if (orderDto.DischargeType == 0)
            {
                errors.Add(new { field = "dischargeType", message = "出院类型不能为空" });
            }

            if (string.IsNullOrWhiteSpace(orderDto.DischargeDiagnosis))
            {
                warnings.Add(new { field = "dischargeDiagnosis", message = "建议填写出院诊断" });
            }

            if (string.IsNullOrWhiteSpace(orderDto.DischargeInstructions))
            {
                warnings.Add(new { field = "dischargeInstructions", message = "建议填写出院医嘱" });
            }

            // 随访验证
            if (orderDto.RequiresFollowUp && !orderDto.FollowUpDate.HasValue)
            {
                errors.Add(new { field = "followUpDate", message = "需要随访时必须填写随访日期" });
            }

            if (orderDto.FollowUpDate.HasValue && orderDto.FollowUpDate.Value <= DateTime.UtcNow)
            {
                errors.Add(new { field = "followUpDate", message = "随访日期必须是将来日期" });
            }

            // 带回药品验证
            if (orderDto.Items != null && orderDto.Items.Any())
            {
                for (int i = 0; i < orderDto.Items.Count; i++)
                {
                    var item = orderDto.Items[i];
                    if (string.IsNullOrWhiteSpace(item.DrugId))
                    {
                        errors.Add(new { field = $"items[{i}].drugId", message = "药品ID不能为空" });
                    }
                    if (string.IsNullOrWhiteSpace(item.Dosage))
                    {
                        errors.Add(new { field = $"items[{i}].dosage", message = "剂量不能为空" });
                    }
                }

                if (!string.IsNullOrWhiteSpace(orderDto.MedicationInstructions))
                {
                    warnings.Add(new 
                    { 
                        field = "medicationInstructions", 
                        message = "建议填写带回药品的用药指导" 
                    });
                }
            }

            return Ok(new
            {
                isValid = errors.Count == 0,
                errors,
                warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 验证出院医嘱数据时发生异常");
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误",
                error = ex.Message
            });
        }
    }
}
