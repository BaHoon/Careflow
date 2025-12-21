using CareFlow.Application.DTOs.Common;
using CareFlow.Application.DTOs.InspectionOrders;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Enums;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services.InspectionOrders;

/// <summary>
/// 检查医嘱服务实现
/// </summary>
public class InspectionOrderService : IInspectionOrderService
{
    private readonly ILogger<InspectionOrderService> _logger;
    private readonly IRepository<InspectionOrder, long> _orderRepo;
    private readonly IRepository<Core.Models.Organization.Patient, string> _patientRepo;
    private readonly IRepository<Core.Models.Organization.Doctor, string> _doctorRepo;
    private readonly IInspectionService _inspectionService;
    private readonly INurseAssignmentService _nurseAssignmentService;

    public InspectionOrderService(
        ILogger<InspectionOrderService> logger,
        IRepository<InspectionOrder, long> orderRepo,
        IRepository<Core.Models.Organization.Patient, string> patientRepo,
        IRepository<Core.Models.Organization.Doctor, string> doctorRepo,
        IInspectionService inspectionService,
        INurseAssignmentService nurseAssignmentService)
    {
        _logger = logger;
        _orderRepo = orderRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _inspectionService = inspectionService;
        _nurseAssignmentService = nurseAssignmentService;
    }

    /// <summary>
    /// 批量创建检查医嘱 DONE
    /// </summary>
    public async Task<BatchCreateOrderResponseDto> BatchCreateOrdersAsync(
        BatchCreateInspectionOrderRequestDto request)
    {
        _logger.LogInformation("开始批量创建检查医嘱，患者ID: {PatientId}, 医嘱数量: {Count}",
            request.PatientId, request.Orders.Count);

        var errors = new List<string>();
        var createdOrders = new List<long>();

        try
        {
            // 验证患者存在
            var patient = await _patientRepo.GetByIdAsync(request.PatientId);
            if (patient == null)
            {
                errors.Add($"患者不存在: {request.PatientId}");
                return new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "患者不存在",
                    Errors = errors
                };
            }

            // 验证医生存在
            var doctor = await _doctorRepo.GetByIdAsync(request.DoctorId);
            if (doctor == null)
            {
                errors.Add($"医生不存在: {request.DoctorId}");
                return new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "医生不存在",
                    Errors = errors
                };
            }

            // 逐条创建检查医嘱
            foreach (var orderDto in request.Orders)
            {
                try
                {
                    // 生成 RisLisId（检查申请单号）
                    var risLisId = GenerateRisLisId(orderDto.ItemCode);

                    // 自动生成预约时间和地点
                    var appointmentTime = GenerateAppointmentTime(orderDto.ItemCode);
                    var appointmentPlace = GetAppointmentPlace(orderDto.ItemCode);

                    var order = new InspectionOrder
                    {
                        PatientId = request.PatientId,
                        DoctorId = request.DoctorId,
                        OrderType = "InspectionOrder",
                        Status = OrderStatus.PendingReceive, // 待护士签收
                        IsLongTerm = false, // 检查医嘱通常为临时医嘱
                        CreateTime = DateTime.UtcNow,
                        PlantEndTime = appointmentTime.AddHours(2), // 预约时间后2小时结束
                        
                        // 检查医嘱特有字段
                        ItemCode = orderDto.ItemCode,
                        RisLisId = risLisId,
                        Location = GetInspectionLocation(orderDto.ItemCode),
                        Source = DetermineInspectionSource(orderDto.ItemCode),
                        InspectionStatus = InspectionOrderStatus.Pending, // 保持待处理状态，等护士接受
                        
                        // 自动生成的预约信息
                        AppointmentTime = appointmentTime,
                        AppointmentPlace = appointmentPlace,
                        Precautions = BuildPrecautions(orderDto),
                        
                        Remarks = orderDto.Remarks
                    };

                    await _orderRepo.AddAsync(order);
                    createdOrders.Add(order.Id);
                    
                    // 分配负责护士
                    await AssignResponsibleNurseAsync(order, request.PatientId);
                    
                    _logger.LogInformation("✅ 创建检查医嘱成功: ID={OrderId}, 项目代码={ItemCode}",
                        order.Id, orderDto.ItemCode);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"创建检查医嘱失败 ({orderDto.ItemCode}): {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, errorMsg);
                }
            }

            if (createdOrders.Count == 0)
            {
                return new BatchCreateOrderResponseDto
                {
                    Success = false,
                    Message = "所有检查医嘱创建失败",
                    Errors = errors
                };
            }

            return new BatchCreateOrderResponseDto
            {
                Success = true,
                Message = $"成功创建 {createdOrders.Count} 条检查医嘱",
                Data = new BatchCreateOrderDataDto
                {
                    CreatedCount = createdOrders.Count,
                    OrderIds = createdOrders.Select(id => id.ToString()).ToList()
                },
                Errors = errors.Count > 0 ? errors : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建检查医嘱时发生异常");
            return new BatchCreateOrderResponseDto
            {
                Success = false,
                Message = "批量创建检查医嘱失败",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// 生成 RIS/LIS 申请单号
    /// </summary>
    private string GenerateRisLisId(string itemCode)
    {
        // 根据项目代码判断是RIS还是LIS
        var prefix = itemCode.Contains("PATH") ? "LIS" : "RIS";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}{timestamp}{random}";
    }

    /// <summary>
    /// 确定检查来源（RIS/LIS）
    /// </summary>
    private InspectionSource DetermineInspectionSource(string itemCode)
    {
        // 病理检查使用LIS，其他使用RIS
        return itemCode.StartsWith("PATH") ? InspectionSource.LIS : InspectionSource.RIS;
    }

    /// <summary>
    /// 根据检查项目代码获取检查科室位置
    /// </summary>
    private string GetInspectionLocation(string itemCode)
    {
        return itemCode.Split('_')[0] switch
        {
            "CT" => "影像科CT室",
            "MRI" => "影像科MRI室",
            "XRAY" => "影像科X光室",
            "DR" => "影像科DR室",
            "PETCT" => "影像科PET-CT室",
            "DSA" => "影像科DSA室",
            "US" => "超声科",
            "ENDO" => "内窥镜中心",
            "ECG" => "功能检查室",
            "EEG" => "功能检查室",
            "PFT" => "功能检查室",
            "PATH" => "病理科",
            _ => "检查科"
        };
    }

    /// <summary>
    /// 自动生成预约时间（根据检查类型设置不同的时间）
    /// </summary>
    private DateTime GenerateAppointmentTime(string itemCode)
    {
        var baseTime = DateTime.UtcNow.AddDays(1); // 默认第二天
        var itemPrefix = itemCode.Split('_')[0];
        
        // 根据检查类型设置不同的预约时间段
        return itemPrefix switch
        {
            "CT" or "MRI" or "PETCT" => baseTime.Date.AddHours(9), // 上午9点
            "XRAY" or "DR" => baseTime.Date.AddHours(14), // 下午2点
            "US" => baseTime.Date.AddHours(8), // 上午8点（超声需要空腹）
            "ENDO" => baseTime.Date.AddHours(8).AddDays(1), // 后天上午8点（内窥镜需要提前准备）
            "ECG" or "EEG" or "PFT" => baseTime.Date.AddHours(10), // 上午10点
            "PATH" => baseTime.Date.AddHours(9), // 上午9点
            _ => baseTime.Date.AddHours(9)
        };
    }

    /// <summary>
    /// 根据检查类型自动分配预约地点
    /// </summary>
    private string GetAppointmentPlace(string itemCode)
    {
        var itemPrefix = itemCode.Split('_')[0];
        
        return itemPrefix switch
        {
            "CT" => "影像科CT室1号机房",
            "MRI" => "影像科MRI室2号机房",
            "XRAY" => "影像科X光室3号诊室",
            "DR" => "影像科DR室4号诊室",
            "PETCT" => "影像科PET-CT中心",
            "DSA" => "影像科DSA介入室",
            "US" => "超声科1号诊室",
            "ENDO" => "内窥镜中心检查室",
            "ECG" => "功能检查室心电图室",
            "EEG" => "功能检查室脑电图室",
            "PFT" => "功能检查室肺功能室",
            "PATH" => "病理科标本接收处",
            _ => "检查科登记处"
        };
    }

    /// <summary>
    /// 构建检查注意事项
    /// </summary>
    private string BuildPrecautions(InspectionOrderDto orderDto)
    {
        var precautions = new List<string>();

        // 如果前端已经提供了注意事项，优先使用
        if (!string.IsNullOrWhiteSpace(orderDto.Precautions))
        {
            return orderDto.Precautions;
        }

        // 根据检查项目代码添加默认注意事项
        var itemPrefix = orderDto.ItemCode.Split('_')[0];
        switch (itemPrefix)
        {
            case "CT":
            case "MRI":
                precautions.Add("检查前需去除金属物品");
                if (orderDto.ItemCode.Contains("ABDOMEN"))
                {
                    precautions.Add("检查前禁食4-6小时");
                }
                break;
            case "US":
                if (orderDto.ItemCode.Contains("ABDOMEN"))
                {
                    precautions.Add("检查前空腹，需憋尿");
                }
                break;
            case "ENDO":
                if (orderDto.ItemCode.Contains("GASTRO"))
                {
                    precautions.Add("检查前8小时禁食，需签署知情同意书");
                }
                else if (orderDto.ItemCode.Contains("COLON"))
                {
                    precautions.Add("检查前需清肠，按医嘱服用泻剂溶液，需签署知情同意书");
                }
                else
                {
                    precautions.Add("需签署知情同意书");
                }
                break;
            case "ECG":
                precautions.Add("检查前避免剧烈运动");
                break;
        }

        return precautions.Count > 0 ? string.Join("; ", precautions) : "无特殊注意事项";
    }

    /// <summary>
    /// 分配负责护士
    /// </summary>
    private async Task AssignResponsibleNurseAsync(
        InspectionOrder order,
        string patientId)
    {
        try
        {
            // 使用当前时间（医嘱创建时间）来分配负责护士，而不是预约时间
            // 负责护士是指接收和处理医嘱的护士，不是预约时间执行检查的护士
            var responsibleNurseId = await _nurseAssignmentService.CalculateResponsibleNurseAsync(
                patientId,
                DateTime.UtcNow);

            if (!string.IsNullOrEmpty(responsibleNurseId))
            {
                order.NurseId = responsibleNurseId;
                await _orderRepo.UpdateAsync(order);
                _logger.LogInformation("✅ 已分配负责护士: {NurseId} 给检查医嘱 {OrderId}",
                    responsibleNurseId, order.Id);
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到负责护士，检查医嘱 {OrderId} 的 NurseId 保持为空", order.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 计算负责护士失败，检查医嘱 {OrderId}", order.Id);
            // 护士分配失败不影响医嘱创建，继续执行
        }
    }
}
