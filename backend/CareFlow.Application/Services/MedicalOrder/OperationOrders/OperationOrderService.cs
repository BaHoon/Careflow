using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.OperationOrders;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CareFlow.Application.Services.MedicalOrder.OperationOrders;

/// <summary>
/// 操作医嘱服务实现（参照药品医嘱）
/// 整合了原 OperationOrderManager 的功能
/// </summary>
public class OperationOrderService : IOperationOrderService, IOperationOrderManager
{
    private readonly IRepository<OperationOrder, long> _orderRepository;
    private readonly IRepository<MedicalOrderStatusHistory, long> _statusHistoryRepository;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly ILogger<OperationOrderService> _logger;

    // 操作代码到操作名称的映射
    private static readonly Dictionary<string, string> OperationNameMap = new()
    {
        // 一、呼吸道管理类
        { "OP001", "持续低流量吸氧" },
        { "OP002", "雾化吸入治疗" },
        { "OP003", "经口/鼻吸痰" },
        { "OP004", "气管切开护理" },
        
        // 二、管路置入与维护类
        { "OP005", "留置胃管(鼻饲管置入)" },
        { "OP006", "胃肠减压护理" },
        { "OP007", "留置导尿术" },
        { "OP008", "更换引流袋/尿袋" },
        { "OP009", "膀胱冲洗" },
        { "OP010", "大量不保留灌肠" },
        
        // 三、静脉与标本采集类
        { "OP011", "快速血糖监测(末梢)" },
        { "OP012", "静脉留置针置管" },
        { "OP013", "静脉采血" },
        { "OP014", "动脉血气采集" },
        { "OP015", "静脉输血护理" },
        
        // 四、伤口与皮肤护理类
        { "OP016", "普通换药/敷料更换" },
        { "OP017", "造口护理" },
        { "OP018", "手术切口拆线" },
        
        // 五、仪器监测与治疗类
        { "OP019", "心电监护" },
        { "OP020", "微量泵/注射泵使用" },
        { "OP021", "气压治疗(预防血栓)" }
    };

    public OperationOrderService(
        IRepository<OperationOrder, long> orderRepository,
        IRepository<MedicalOrderStatusHistory, long> statusHistoryRepository,
        INurseAssignmentService nurseAssignmentService,
        ILogger<OperationOrderService> logger)
    {
        _orderRepository = orderRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _nurseAssignmentService = nurseAssignmentService;
        _logger = logger;
    }

    /// <summary>
    /// 批量创建操作医嘱（参照药品医嘱实现）
    /// </summary>
    public async Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(
        BatchCreateOperationOrderRequestDto request)
    {
        _logger.LogInformation("==================== 开始批量创建操作医嘱 ====================");
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
                _logger.LogInformation("处理医嘱: OpId={OpId}, 策略={Strategy}, 类型={Type}",
                    orderDto.OpId,
                    orderDto.TimingStrategy,
                    orderDto.IsLongTerm ? "长期" : "临时");

                // 业务验证
                var validationResult = ValidateOrderDto(orderDto);
                if (!validationResult.isValid)
                {
                    errors.AddRange(validationResult.errors);
                    continue;
                }

                // 创建医嘱实体
                var order = CreateOrderEntity(request.PatientId, request.DoctorId, orderDto);

                // 保存医嘱
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
                    Reason = "医生创建操作医嘱"
                };
                await _statusHistoryRepository.AddAsync(history);

                _logger.LogInformation("✅ 成功创建操作医嘱，ID: {OrderId}, OpId: {OpId}",
                    order.Id, order.OpId);
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
                TaskCount = 0  // 创建时不生成任务
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
    private (bool isValid, List<string> errors) ValidateOrderDto(OperationOrderDto orderDto)
    {
        var errors = new List<string>();

        // 基础验证
        if (string.IsNullOrWhiteSpace(orderDto.OpId))
        {
            errors.Add("操作代码不能为空");
        }

        if (string.IsNullOrWhiteSpace(orderDto.OperationName))
        {
            errors.Add("操作名称不能为空");
        }

        if (orderDto.PlantEndTime == default)
        {
            errors.Add("医嘱结束时间不能为空");
        }

        // 时间策略验证
        if (string.IsNullOrWhiteSpace(orderDto.TimingStrategy))
        {
            errors.Add("时间策略不能为空");
        }
        else
        {
            switch (orderDto.TimingStrategy.ToUpper())
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
        }

        // 时间验证
        if (orderDto.PlantEndTime <= DateTime.UtcNow)
        {
            errors.Add("医嘱结束时间不能早于当前时间");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 创建医嘱实体
    /// </summary>
    private OperationOrder CreateOrderEntity(
        string patientId,
        string doctorId,
        OperationOrderDto orderDto)
    {
        // 如果没有提供OperationName，根据OpId自动填充
        var operationName = orderDto.OperationName;
        if (string.IsNullOrWhiteSpace(operationName))
        {
            operationName = OperationNameMap.ContainsKey(orderDto.OpId)
                ? OperationNameMap[orderDto.OpId]
                : $"操作 {orderDto.OpId}";
        }

        var order = new OperationOrder
        {
            PatientId = patientId,
            DoctorId = doctorId,
            OrderType = "OperationOrder",
            IsLongTerm = orderDto.IsLongTerm,
            Status = OrderStatus.PendingReceive,
            CreateTime = DateTime.UtcNow,

            // 操作信息
            OpId = orderDto.OpId,
            OperationName = operationName,
            OperationSite = orderDto.OperationSite,
            Normal = orderDto.Normal,

            // 时间策略字段（前端发送的北京时间会被.NET自动转换为UTC）
            TimingStrategy = orderDto.TimingStrategy,
            StartTime = orderDto.StartTime?.ToUniversalTime(),
            PlantEndTime = orderDto.PlantEndTime.ToUniversalTime(),
            IntervalHours = orderDto.IntervalHours,
            IntervalDays = orderDto.IntervalDays,
            SmartSlotsMask = orderDto.SmartSlotsMask,

            // 执行要求
            OperationRequirements = orderDto.OperationRequirements != null
                ? JsonSerializer.Serialize(orderDto.OperationRequirements)
                : null,
            RequiresPreparation = orderDto.RequiresPreparation,
            PreparationItems = orderDto.PreparationItems != null
                ? JsonSerializer.Serialize(orderDto.PreparationItems)
                : null,

            // 任务配置
            ExpectedDurationMinutes = orderDto.ExpectedDurationMinutes,
            RequiresResult = orderDto.RequiresResult,
            ResultTemplate = orderDto.ResultTemplate != null
                ? JsonSerializer.Serialize(orderDto.ResultTemplate)
                : null,

            Remarks = string.IsNullOrWhiteSpace(orderDto.Remarks) ? null : orderDto.Remarks
        };

        return order;
    }

    /// <summary>
    /// 分配负责护士
    /// </summary>
    private async Task AssignResponsibleNurseAsync(
        OperationOrder order,
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

    /// <summary>
    /// 根据操作代码获取操作名称
    /// </summary>
    private string GetOperationNameFromOpId(string opId)
    {
        return OperationNameMap.ContainsKey(opId) 
            ? OperationNameMap[opId] 
            : $"操作 {opId}";
    }

    /// <summary>
    /// 创建单个操作医嘱（兼容旧接口，原 OperationOrderManager 的功能）
    /// </summary>
    public async Task<OperationOrder> CreateOperationOrderAsync(OperationOrder order)
    {
        // 1. 设置默认值
        if (order.Status == OrderStatus.Draft || string.IsNullOrEmpty(order.Status.ToString()))
        {
            order.Status = OrderStatus.PendingReceive; // 初始状态
        }

        if (order.CreateTime == default)
        {
            order.CreateTime = DateTime.UtcNow;
        }

        // 如果没有提供OperationName，根据OpId自动填充
        if (string.IsNullOrWhiteSpace(order.OperationName))
        {
            order.OperationName = GetOperationNameFromOpId(order.OpId);
        }

        // 2. 先保存到数据库（确保实体有ID，用于后续更新）
        await _orderRepository.AddAsync(order);

        _logger.LogInformation("操作医嘱已保存到数据库: OrderId={OrderId}, OpId={OpId}, PatientId={PatientId}", 
            order.Id, order.OpId, order.PatientId);

        // 3. 插入初始状态历史记录
        var history = new MedicalOrderStatusHistory
        {
            MedicalOrderId = order.Id,
            FromStatus = OrderStatus.Draft,
            ToStatus = OrderStatus.PendingReceive,
            ChangedAt = DateTime.UtcNow,
            ChangedById = order.DoctorId,
            ChangedByType = "Doctor",
            Reason = "医生创建操作医嘱"
        };
        await _statusHistoryRepository.AddAsync(history);

        // 4. 自动分配护士（如果未指定）
        if (string.IsNullOrEmpty(order.NurseId))
        {
            try
            {
                var responsibleNurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(order.PatientId, order.StartTime ?? DateTime.UtcNow);
                
                if (!string.IsNullOrEmpty(responsibleNurseId))
                {
                    order.NurseId = responsibleNurseId;
                    await _orderRepository.UpdateAsync(order);
                    _logger.LogInformation("✅ 已分配负责护士: {NurseId} 给操作医嘱 {OrderId}", 
                        responsibleNurseId, order.Id);
                }
                else
                {
                    _logger.LogWarning("⚠️ 未找到负责护士，操作医嘱 {OrderId} 的 NurseId 将保持为空", order.Id);
                }
            }
            catch (Exception nurseEx)
            {
                _logger.LogError(nurseEx, "❌ 计算负责护士失败，操作医嘱 {OrderId}", order.Id);
            }
        }
        else
        {
            _logger.LogInformation("操作医嘱 {OrderId} 已指定护士: {NurseId}", order.Id, order.NurseId);
        }

        _logger.LogInformation("操作医嘱 {OrderId} 创建完成，等待签收后生成执行任务", order.Id);

        return order;
    }
}
