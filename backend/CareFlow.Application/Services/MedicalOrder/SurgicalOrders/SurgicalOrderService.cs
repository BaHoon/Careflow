using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.SurgicalOrders;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CareFlow.Application.Services.MedicalOrder.SurgicalOrders;

/// <summary>
/// 手术医嘱服务实现
/// </summary>
public class SurgicalOrderService : ISurgicalOrderService
{
    private readonly IRepository<SurgicalOrder, long> _orderRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<SurgicalOrderService> _logger;

    public SurgicalOrderService(
        IRepository<SurgicalOrder, long> orderRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        INurseAssignmentService nurseAssignmentService,
        ILogger<SurgicalOrderService> logger)
    {
        _orderRepository = orderRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _nurseAssignmentService = nurseAssignmentService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建手术医嘱
    /// </summary>
    public async Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(
        BatchCreateSurgicalOrderRequestDto request)
    {
        _logger.LogInformation("==================== 开始批量创建手术医嘱 ====================");
        _logger.LogInformation("患者ID: {PatientId}, 医生ID: {DoctorId}, 医嘱数量: {Count}",
            request.PatientId, request.DoctorId, request.Orders.Count);

        // 参数验证
        if (request.Orders == null || request.Orders.Count == 0)
        {
            return new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "医嘱列表不能为空",
                Errors = new List<string> { "orders字段不能为空" }
            };
        }

        var createdOrderIds = new List<string>();
        var errors = new List<string>();

        foreach (var orderDto in request.Orders)
        {
            try
            {
                _logger.LogInformation("处理手术医嘱: 手术名称={SurgeryName}, 主刀医生={SurgeonId}",
                    orderDto.SurgeryName, orderDto.SurgeonId);

                // 业务验证
                var validationResult = ValidateOrderDto(orderDto);
                if (!validationResult.isValid)
                {
                    errors.AddRange(validationResult.errors);
                    continue;
                }

                // 创建医嘱实体
                var order = CreateOrderEntity(request.PatientId, request.DoctorId, orderDto);

                // 保存医嘱（EF Core 会自动级联保存 Items 集合，如有术前用药）
                _logger.LogInformation("保存手术医嘱到数据库，OrderId将自动生成");
                await _orderRepository.AddAsync(order);
                
                // 插入初始状态历史记录
                var history = new MedicalOrderStatusHistory
                {
                    MedicalOrderId = order.Id,
                    FromStatus = OrderStatus.Draft,
                    ToStatus = OrderStatus.PendingReceive,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = request.DoctorId,
                    ChangedByType = "Doctor",
                    Reason = "医生创建手术医嘱"
                };
                await _statusHistoryRepository.AddAsync(history);

                _logger.LogInformation("✅ 成功创建手术医嘱，ID: {OrderId}, 手术名称: {SurgeryName}",
                    order.Id, order.SurgeryName);
                createdOrderIds.Add(order.Id.ToString());

                // 分配负责护士
                await AssignResponsibleNurseAsync(order, request.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 创建单个手术医嘱失败");
                errors.Add($"手术医嘱创建失败: {ex.Message}");
            }
        }

        // 构造响应
        var response = new BatchCreateOrderResponseDto
        {
            Success = createdOrderIds.Count > 0,
            Message = errors.Count > 0
                ? $"成功创建{createdOrderIds.Count}条手术医嘱，{errors.Count}条失败"
                : $"成功创建{createdOrderIds.Count}条手术医嘱",
            Data = new BatchCreateOrderDataDto
            {
                CreatedCount = createdOrderIds.Count,
                OrderIds = createdOrderIds,
                TaskCount = 0  // TODO: 可对接任务生成服务
            },
            Errors = errors.Count > 0 ? errors : null
        };

        _logger.LogInformation("批量创建完成，成功: {Success}, 失败: {Failed}",
            createdOrderIds.Count, errors.Count);
        _logger.LogInformation("========================================================");

        return response;
    }

    /// <summary>
    /// 验证手术医嘱DTO
    /// </summary>
    private (bool isValid, List<string> errors) ValidateOrderDto(SurgicalOrderDto orderDto)
    {
        var errors = new List<string>();

        // 基础验证
        if (string.IsNullOrWhiteSpace(orderDto.SurgeryName))
        {
            errors.Add("手术名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(orderDto.SurgeonId))
        {
            errors.Add("主刀医生不能为空");
        }
        
        if (string.IsNullOrWhiteSpace(orderDto.AnesthesiaType))
        {
            errors.Add("麻醉方式不能为空");
        }
        
        if (string.IsNullOrWhiteSpace(orderDto.IncisionSite))
        {
            errors.Add("切口部位不能为空");
        }

        if (orderDto.ScheduleTime == default)
        {
            errors.Add("手术时间不能为空");
        }
        else if (orderDto.ScheduleTime < DateTime.UtcNow.AddHours(-1)) // 允许1小时容错
        {
            errors.Add("手术时间不能早于当前时间");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 创建手术医嘱实体
    /// </summary>
    private SurgicalOrder CreateOrderEntity(
        string patientId,
        string doctorId,
        SurgicalOrderDto orderDto)
    {
        var order = new SurgicalOrder
        {
            PatientId = patientId,
            DoctorId = doctorId,
            OrderType = "SurgicalOrder",
            IsLongTerm = false, // 手术医嘱通常为临时医嘱
            Status = OrderStatus.PendingReceive,
            CreateTime = DateTime.UtcNow,

            // 手术基础信息
            SurgeryName = orderDto.SurgeryName,
            ScheduleTime = orderDto.ScheduleTime.ToUniversalTime(),
            AnesthesiaType = orderDto.AnesthesiaType,
            IncisionSite = orderDto.IncisionSite,
            
            // 医生信息
            SurgeonId = orderDto.SurgeonId,
            
            // 医嘱结束时间（手术当天结束）
            PlantEndTime = orderDto.ScheduleTime.Date.AddDays(1).ToUniversalTime(),
            
            // 术前准备 - 使用不转义Unicode的JSON选项
            RequiredTalk = orderDto.RequiredTalk != null && orderDto.RequiredTalk.Any() 
                ? System.Text.Json.JsonSerializer.Serialize(orderDto.RequiredTalk, new System.Text.Json.JsonSerializerOptions 
                { 
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
                }) 
                : null,
            RequiredOperation = orderDto.RequiredOperation != null && orderDto.RequiredOperation.Any() 
                ? System.Text.Json.JsonSerializer.Serialize(orderDto.RequiredOperation, new System.Text.Json.JsonSerializerOptions 
                { 
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
                }) 
                : null,
            
            // 术前准备状态
            PrepProgress = 0.0f,
            PrepStatus = "未开始",

            Remarks = string.IsNullOrWhiteSpace(orderDto.Remarks) ? null : orderDto.Remarks,

            // 初始化手术药品集合
            Items = orderDto.Items?.Select(item => new MedicationOrderItem
            {
                DrugId = item.DrugId,
                Dosage = item.Dosage,
                Note = item.Note ?? string.Empty
            }).ToList() ?? new List<MedicationOrderItem>()
        };

        _logger.LogDebug("创建手术医嘱实体: {SurgeryName}, 主刀医生: {SurgeonId}, 手术时间: {ScheduleTime}",
            order.SurgeryName, order.SurgeonId, order.ScheduleTime);

        return order;
    }

    /// <summary>
    /// 分配负责护士
    /// </summary>
    private async Task AssignResponsibleNurseAsync(
        SurgicalOrder order,
        string patientId)
    {
        try
        {
            // 使用当前时间（医嘱创建时间）来分配负责护士，而不是预约时间
            var responsibleNurseId = await _nurseAssignmentService.CalculateResponsibleNurseAsync(
                patientId,
                DateTime.UtcNow);

            if (!string.IsNullOrEmpty(responsibleNurseId))
            {
                order.NurseId = responsibleNurseId;
                await _orderRepository.UpdateAsync(order);
                _logger.LogInformation("✅ 已分配负责护士: {NurseId} 给手术医嘱 {OrderId}",
                    responsibleNurseId, order.Id);
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到负责护士，手术医嘱 {OrderId} 的 NurseId 保持为空", order.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 计算负责护士失败，手术医嘱 {OrderId}", order.Id);
            // 护士分配失败不影响医嘱创建，继续执行
        }
    }
}
