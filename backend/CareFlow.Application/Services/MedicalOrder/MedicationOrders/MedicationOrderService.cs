using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.MedicationOrders;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services.MedicationOrders;

/// <summary>
/// 药物医嘱服务实现
/// </summary>
public class MedicationOrderService : IMedicationOrderService
{
    private readonly IRepository<Core.Models.Medical.MedicationOrder, long> _orderRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<MedicationOrderService> _logger;

    public MedicationOrderService(
        IRepository<Core.Models.Medical.MedicationOrder, long> orderRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        INurseAssignmentService nurseAssignmentService,
        ILogger<MedicationOrderService> logger)
    {
        _orderRepository = orderRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _nurseAssignmentService = nurseAssignmentService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建药物医嘱
    /// </summary>
    public async Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(
        BatchCreateMedicationOrderRequestDto request)
    {
        _logger.LogInformation("==================== 开始批量创建药物医嘱 ====================");
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
                _logger.LogInformation("处理医嘱: 类型={Type}, 策略={Strategy}, Items数量={ItemCount}",
                    orderDto.IsLongTerm ? "长期" : "临时",
                    orderDto.TimingStrategy,
                    orderDto.Items?.Count ?? 0);

                // 业务验证
                var validationResult = ValidateOrderDto(orderDto);
                if (!validationResult.isValid)
                {
                    errors.AddRange(validationResult.errors);
                    continue;
                }

                // 创建医嘱实体
                var order = CreateOrderEntity(request.PatientId, request.DoctorId, orderDto);

                // 保存医嘱（EF Core 会自动级联保存 Items 集合）
                _logger.LogInformation("保存医嘱到数据库，OrderId将自动生成");
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
                    Reason = "医生创建医嘱"
                };
                await _statusHistoryRepository.AddAsync(history);

                _logger.LogInformation("✅ 成功创建药物医嘱，ID: {OrderId}, Items数量: {ItemCount}",
                    order.Id, order.Items?.Count ?? 0);
                createdOrderIds.Add(order.Id.ToString());

                // 分配负责护士
                await AssignResponsibleNurseAsync(order, request.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 创建单个医嘱失败");
                errors.Add($"医嘱创建失败: {ex.Message}");
            }
        }

        // 构造响应
        var response = new BatchCreateOrderResponseDto
        {
            Success = createdOrderIds.Count > 0,
            Message = errors.Count > 0
                ? $"成功创建{createdOrderIds.Count}条医嘱，{errors.Count}条失败"
                : $"成功创建{createdOrderIds.Count}条医嘱",
            Data = new BatchCreateOrderDataDto
            {
                CreatedCount = createdOrderIds.Count,
                OrderIds = createdOrderIds,
                TaskCount = 0  // TODO: 从任务生成服务获取
            },
            Errors = errors.Count > 0 ? errors : null
        };

        _logger.LogInformation("批量创建完成，成功: {Success}, 失败: {Failed}",
            createdOrderIds.Count, errors.Count);
        _logger.LogInformation("========================================================");

        return response;
    }

    /// <summary>
    /// 验证医嘱DTO
    /// </summary>
    private (bool isValid, List<string> errors) ValidateOrderDto(MedicationOrderDto orderDto)
    {
        var errors = new List<string>();

        // 基础验证
        if (string.IsNullOrWhiteSpace(orderDto.TimingStrategy))
        {
            errors.Add("执行策略不能为空");
        }

        if (orderDto.PlantEndTime == default)
        {
            errors.Add("医嘱结束时间不能为空");
        }

        // 策略特定验证
        switch (orderDto.TimingStrategy?.ToUpper())
        {
            case "SPECIFIC":
                if (orderDto.StartTime == null)
                {
                    errors.Add("指定时间策略需要设置开始时间");
                }
                break;

            case "CYCLIC":
                if (orderDto.StartTime == null)
                {
                    errors.Add("周期策略需要设置开始时间");
                }
                if (!orderDto.IntervalHours.HasValue || orderDto.IntervalHours <= 0)
                {
                    errors.Add("周期策略需要设置间隔小时数");
                }
                break;

            case "SLOTS":
                if (orderDto.StartTime == null)
                {
                    errors.Add("时段策略需要设置开始时间");
                }
                if (orderDto.SmartSlotsMask <= 0)
                {
                    errors.Add("时段策略需要选择至少一个时段");
                }
                break;
        }

        // 药品验证
        if (orderDto.Items == null || orderDto.Items.Count == 0)
        {
            errors.Add("至少需要添加一个药品");
        }
        else
        {
            foreach (var item in orderDto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.DrugId))
                {
                    errors.Add("药品ID不能为空");
                }
                if (string.IsNullOrWhiteSpace(item.Dosage))
                {
                    errors.Add("剂量不能为空");
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 创建医嘱实体
    /// </summary>
    private Core.Models.Medical.MedicationOrder CreateOrderEntity(
        string patientId,
        string doctorId,
        MedicationOrderDto orderDto)
    {
        var order = new Core.Models.Medical.MedicationOrder
        {
            PatientId = patientId,
            DoctorId = doctorId,
            OrderType = "MedicationOrder",
            IsLongTerm = orderDto.IsLongTerm,
            Status = OrderStatus.PendingReceive,
            CreateTime = DateTime.UtcNow,

            // 时间策略字段（前端发送的北京时间会被.NET自动转换为UTC）
            TimingStrategy = orderDto.TimingStrategy,
            StartTime = orderDto.StartTime?.ToUniversalTime(),
            PlantEndTime = orderDto.PlantEndTime.ToUniversalTime(),
            IntervalHours = orderDto.IntervalHours,
            IntervalDays = orderDto.IntervalDays,
            SmartSlotsMask = orderDto.SmartSlotsMask,

            // 给药途径
            UsageRoute = (UsageRoute)orderDto.UsageRoute,

            Remarks = string.IsNullOrWhiteSpace(orderDto.Remarks) ? null : orderDto.Remarks,

            // 初始化 Items 集合
            Items = new List<MedicationOrderItem>()
        };

        // 创建药品项
        if (orderDto.Items != null && orderDto.Items.Count > 0)
        {
            _logger.LogInformation("创建 {Count} 个药品项目", orderDto.Items.Count);

            foreach (var itemDto in orderDto.Items)
            {
                var orderItem = new MedicationOrderItem
                {
                    DrugId = itemDto.DrugId,
                    Dosage = itemDto.Dosage,
                    Note = string.IsNullOrWhiteSpace(itemDto.Note) ? string.Empty : itemDto.Note,
                    CreateTime = DateTime.UtcNow
                };

                order.Items.Add(orderItem);

                _logger.LogDebug("添加药品: DrugId={DrugId}, Dosage={Dosage}",
                    orderItem.DrugId, orderItem.Dosage);
            }
        }

        return order;
    }

    /// <summary>
    /// 分配负责护士
    /// </summary>
    private async Task AssignResponsibleNurseAsync(
        Core.Models.Medical.MedicationOrder order,
        string patientId)
    {
        try
        {
            var responsibleNurseId = await _nurseAssignmentService.CalculateResponsibleNurseAsync(
                patientId,
                order.StartTime ?? DateTime.UtcNow);

            if (!string.IsNullOrEmpty(responsibleNurseId))
            {
                order.NurseId = responsibleNurseId;
                await _orderRepository.UpdateAsync(order);
                _logger.LogInformation("✅ 已分配负责护士: {NurseId} 给医嘱 {OrderId}",
                    responsibleNurseId, order.Id);
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到负责护士，医嘱 {OrderId} 的 NurseId 保持为空", order.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 计算负责护士失败，医嘱 {OrderId}", order.Id);
            // 护士分配失败不影响医嘱创建，继续执行
        }
    }
}
