using CareFlow.Application.DTOs.Patient;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 患者日志服务接口
/// </summary>
public interface IPatientLogService
{
    /// <summary>
    /// 获取患者日志数据 (医嘱执行、护理记录、检查报告的时间线汇总)
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>患者日志响应DTO</returns>
    Task<PatientLogResponseDto> GetPatientLogAsync(PatientLogQueryDto query);
}
