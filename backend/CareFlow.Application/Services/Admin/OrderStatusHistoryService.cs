using CareFlow.Application.DTOs.Admin;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PatientModel = CareFlow.Core.Models.Organization.Patient;

namespace CareFlow.Application.Services.Admin;

/// <summary>
/// 医嘱状态历史查询服务
/// </summary>
public class OrderStatusHistoryService
{
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicalOrderStatusHistory, long> _historyRepository;
    private readonly IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> _orderRepository;
    private readonly IRepository<PatientModel, string> _patientRepository;
    private readonly ILogger<OrderStatusHistoryService> _logger;

    public OrderStatusHistoryService(
        IRepository<CareFlow.Core.Models.Medical.MedicalOrderStatusHistory, long> historyRepository,
        IRepository<CareFlow.Core.Models.Medical.MedicalOrder, long> orderRepository,
        IRepository<PatientModel, string> patientRepository,
        ILogger<OrderStatusHistoryService> logger)
    {
        _historyRepository = historyRepository;
        _orderRepository = orderRepository;
        _patientRepository = patientRepository;
        _logger = logger;
    }

    /// <summary>
    /// 查询医嘱状态历史记录
    /// </summary>
    public async Task<QueryOrderStatusHistoryResponseDto> QueryHistoriesAsync(QueryOrderStatusHistoryRequestDto request)
    {
        _logger.LogInformation("查询医嘱状态历史，筛选条件: {@Request}", request);

        // 构建查询
        var query = _historyRepository.GetQueryable()
            .Include(h => h.MedicalOrder)
                .ThenInclude(o => o.Patient)
            .AsQueryable();

        // 筛选条件：患者ID
        if (!string.IsNullOrEmpty(request.PatientId))
        {
            query = query.Where(h => h.MedicalOrder.PatientId == request.PatientId);
        }

        // 筛选条件：患者姓名
        if (!string.IsNullOrEmpty(request.PatientName))
        {
            query = query.Where(h => h.MedicalOrder.Patient.Name.Contains(request.PatientName));
        }

        // 筛选条件：操作人
        if (!string.IsNullOrEmpty(request.ChangedById))
        {
            query = query.Where(h => h.ChangedById == request.ChangedById);
        }

        // 筛选条件：操作人类型
        if (!string.IsNullOrEmpty(request.ChangedByType))
        {
            query = query.Where(h => h.ChangedByType == request.ChangedByType);
        }

        // 筛选条件：医嘱类型
        if (!string.IsNullOrEmpty(request.OrderType))
        {
            query = query.Where(h => h.MedicalOrder.OrderType == request.OrderType);
        }

        // 筛选条件：时间范围
        if (request.StartTime.HasValue)
        {
            query = query.Where(h => h.ChangedAt >= request.StartTime.Value);
        }
        if (request.EndTime.HasValue)
        {
            query = query.Where(h => h.ChangedAt <= request.EndTime.Value);
        }

        // 总数
        var totalCount = await query.CountAsync();

        // 排序和分页
        var histories = await query
            .OrderByDescending(h => h.ChangedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // 映射到DTO
        var historyDtos = new List<OrderStatusHistoryDto>();
        foreach (var history in histories)
        {
            var dto = new OrderStatusHistoryDto
            {
                Id = history.Id,
                MedicalOrderId = history.MedicalOrderId,
                OrderType = history.MedicalOrder?.OrderType ?? "Unknown",
                OrderSummary = await GenerateOrderSummaryAsync(history.MedicalOrder),
                PatientId = history.MedicalOrder?.PatientId ?? "",
                PatientName = history.MedicalOrder?.Patient?.Name ?? "未知患者",
                BedId = history.MedicalOrder?.Patient?.BedId ?? "",
                FromStatus = (int)history.FromStatus,
                FromStatusName = GetStatusName(history.FromStatus),
                ToStatus = (int)history.ToStatus,
                ToStatusName = GetStatusName(history.ToStatus),
                ChangedAt = history.ChangedAt,
                ChangedById = history.ChangedById,
                ChangedByName = history.ChangedByName ?? history.ChangedById, // 如果姓名为空，显示编号
                ChangedByType = history.ChangedByType,
                Reason = history.Reason,
                Notes = history.Notes
            };
            historyDtos.Add(dto);
        }

        return new QueryOrderStatusHistoryResponseDto
        {
            Histories = historyDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 生成医嘱摘要
    /// </summary>
    private async Task<string> GenerateOrderSummaryAsync(CareFlow.Core.Models.Medical.MedicalOrder? order)
    {
        if (order == null) return "未知医嘱";

        return order.OrderType switch
        {
            "MedicationOrder" => "药品医嘱",
            "InspectionOrder" => "检查医嘱",
            "OperationOrder" => "操作医嘱",
            "SurgicalOrder" => "手术医嘱",
            "DischargeOrder" => "出院医嘱",
            _ => "其他医嘱"
        };
    }

    /// <summary>
    /// 获取状态名称
    /// </summary>
    private string GetStatusName(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Draft => "草稿",
            OrderStatus.PendingReceive => "未签收",
            OrderStatus.Accepted => "已签收",
            OrderStatus.InProgress => "进行中",
            OrderStatus.Completed => "已完成",
            OrderStatus.Rejected => "已拒绝",
            OrderStatus.Cancelled => "已取消",
            OrderStatus.PendingStop => "等待停嘱",
            OrderStatus.Stopped => "已停止",
            OrderStatus.StoppingInProgress => "停止中",
            OrderStatus.Abnormal => "异常态",
            _ => $"状态{(int)status}"
        };
    }
}
