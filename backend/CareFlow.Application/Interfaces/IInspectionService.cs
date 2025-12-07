using CareFlow.Application.DTOs.Inspection;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 检查类医嘱服务接口
/// </summary>
public interface IInspectionService
{
    // ===== 检查医嘱相关 =====
    
    /// <summary>
    /// 创建检查医嘱
    /// </summary>
    Task<long> CreateInspectionOrderAsync(CreateInspectionOrderDto dto);
    
    /// <summary>
    /// 更新预约信息 (模拟接收 RIS/LIS 的预约反馈)
    /// </summary>
    Task UpdateAppointmentAsync(UpdateAppointmentDto dto);
    
    /// <summary>
    /// 更新检查状态
    /// </summary>
    Task UpdateInspectionStatusAsync(UpdateInspectionStatusDto dto);
    
    /// <summary>
    /// 获取检查医嘱详情
    /// </summary>
    Task<InspectionOrderDetailDto> GetInspectionOrderDetailAsync(long orderId);
    
    /// <summary>
    /// 生成检查导引单
    /// </summary>
    Task<InspectionGuideDto> GenerateInspectionGuideAsync(long orderId);
    
    /// <summary>
    /// 获取患者的所有检查医嘱
    /// </summary>
    Task<List<InspectionOrderDetailDto>> GetPatientInspectionOrdersAsync(string patientId);
    
    /// <summary>
    /// 获取护士看板数据 (按病区查询)
    /// </summary>
    Task<List<NurseInspectionBoardDto>> GetNurseBoardDataAsync(string wardId);
    
    // ===== 检查报告相关 =====
    
    /// <summary>
    /// 创建检查报告 (模拟从 RIS/LIS 接收报告)
    /// </summary>
    Task<long> CreateInspectionReportAsync(CreateInspectionReportDto dto);
    
    /// <summary>
    /// 获取检查报告详情
    /// </summary>
    Task<InspectionReportDetailDto> GetInspectionReportDetailAsync(long reportId);
    
    /// <summary>
    /// 根据医嘱ID获取报告列表
    /// </summary>
    Task<List<InspectionReportDetailDto>> GetReportsByOrderIdAsync(long orderId);
    
    /// <summary>
    /// 更新报告状态
    /// </summary>
    Task UpdateReportStatusAsync(UpdateReportStatusDto dto);
    
    // ===== 测试数据生成 =====
    
    /// <summary>
    /// 生成模拟检查医嘱数据
    /// </summary>
    Task GenerateMockInspectionOrdersAsync();
}
