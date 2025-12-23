using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.DTOs.OrderApplication;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

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
    
    // ===== 任务生成相关 =====
    
    /// <summary>
    /// 生成检查申请任务（签收时调用）
    /// </summary>
    /// <param name="order">检查医嘱</param>
    /// <returns>申请任务</returns>
    Task<ExecutionTask> GenerateApplicationTaskAsync(InspectionOrder order);
    
    /// <summary>
    /// 根据预约信息生成执行任务（预约确认后调用）
    /// </summary>
    /// <param name="orderId">检查医嘱ID</param>
    /// <param name="appointmentDetail">预约详情</param>
    /// <returns>生成的执行任务列表（打印导引单、患者签到、检查完成）</returns>
    Task<List<ExecutionTask>> GenerateExecutionTasksAsync(long orderId, AppointmentDetail appointmentDetail);
}
