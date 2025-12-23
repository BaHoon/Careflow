using Microsoft.AspNetCore.Mvc;
using CareFlow.Application.Services;
using CareFlow.Application.DTOs.OperationOrder;
using CareFlow.Core.Models.Medical;
using Microsoft.Extensions.Logging;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 操作医嘱控制器（独立入口，不修改 MedicalOrderController）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OperationOrderController : ControllerBase
{
    private readonly IOperationOrderManager _orderManager;
    private readonly ILogger<OperationOrderController> _logger;

    public OperationOrderController(
        IOperationOrderManager orderManager,
        ILogger<OperationOrderController> logger)
    {
        _orderManager = orderManager;
        _logger = logger;
    }

    /// <summary>
    /// 创建操作医嘱（自动分配护士）
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateOperationOrder([FromBody] CreateOperationOrderRequestDto request)
    {
        try
        {
            _logger.LogInformation("开始创建操作医嘱: OpId={OpId}, PatientId={PatientId}", 
                request.OpId, request.PatientId);

            // 1. 创建操作医嘱实体
            var operationOrder = new OperationOrder
            {
                // 基础字段
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                OrderType = "OperationOrder",
                IsLongTerm = request.IsLongTerm,
                PlantEndTime = request.PlantEndTime,
                Remarks = request.Remarks,
                
                // 操作特有字段
                OpId = request.OpId,
                Normal = request.Normal,
                FrequencyType = request.FrequencyType,
                FrequencyValue = request.FrequencyValue
            };

            // 2. 调用管理服务创建医嘱（自动分配护士）
            var createdOrder = await _orderManager.CreateOperationOrderAsync(operationOrder);

            // 3. 返回结果
            return Ok(new 
            { 
                Success = true, 
                Message = "操作医嘱创建成功，已自动分配护士并生成执行任务", 
                OrderId = createdOrder.Id, 
                AssignedNurseId = createdOrder.NurseId,
                OpId = createdOrder.OpId,
                FrequencyType = createdOrder.FrequencyType,
                FrequencyValue = createdOrder.FrequencyValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建操作医嘱失败");
            return BadRequest(new 
            { 
                Success = false, 
                Message = $"创建操作医嘱失败: {ex.Message}" 
            });
        }
    }
}

