using CareFlow.Application.DTOs.Inspection;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 检查类医嘱服务接口
/// </summary>
public interface IInspectionService
{
    // ===== 检查医嘱状态管理(内部使用) =====
    
    /// <summary>
    /// 更新检查状态(内部使用,由扫码等操作调用)
    /// </summary>
    Task UpdateInspectionStatusAsync(UpdateInspectionStatusDto dto);
    
    /// <summary>
    /// 获取检查医嘱详情
    /// </summary>
    Task<InspectionOrderDetailDto> GetInspectionOrderDetailAsync(long orderId);
    
    /// <summary>
    /// 病房护士发送检查申请到检查站
    /// </summary>
    Task SendInspectionRequestAsync(SendInspectionRequestDto dto);
    
    /// <summary>
    /// 检查站护士查看待处理的检查申请列表
    /// </summary>
    Task<List<InspectionRequestDto>> GetPendingRequestsAsync(string inspectionStationId);
    
    /// <summary>
    /// 检查站护士创建预约(自动生成3个执行任务)
    /// </summary>
    Task CreateAppointmentAsync(CreateAppointmentDto dto);
    
    /// <summary>
    /// 统一扫码处理(根据任务类型自动处理)
    /// </summary>
    Task<object> ProcessScanAsync(SingleScanDto dto);
    
    // ===== 检查报告相关 =====
    
    /// <summary>
    /// 创建检查报告
    /// </summary>
    Task<long> CreateInspectionReportAsync(CreateInspectionReportDto dto);
    
    /// <summary>
    /// 获取检查报告详情
    /// </summary>
    Task<InspectionReportDetailDto> GetInspectionReportDetailAsync(long reportId);
}
