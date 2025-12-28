using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.DTOs.Common;

namespace CareFlow.Application.Interfaces
{
    /// <summary>
    /// 系统日志服务接口
    /// </summary>
    public interface ISystemLogService
    {
        /// <summary>
        /// 记录系统日志
        /// </summary>
        Task LogAsync(CreateSystemLogDto dto);

        /// <summary>
        /// 查询系统日志
        /// </summary>
        Task<PagedResultDto<SystemLogDto>> GetSystemLogsAsync(QuerySystemLogRequestDto request);

        /// <summary>
        /// 记录登录日志
        /// </summary>
        Task LogLoginAsync(int? operatorId, string? operatorName, string? ipAddress, bool success, string? errorMessage = null);

        /// <summary>
        /// 记录登出日志
        /// </summary>
        Task LogLogoutAsync(int? operatorId, string? operatorName, string? ipAddress);

        /// <summary>
        /// 记录医嘱停止日志
        /// </summary>
        Task LogOrderStopAsync(int? operatorId, string? operatorName, int orderId, string? reason, string? ipAddress = null);

        /// <summary>
        /// 记录药品核对失败日志
        /// </summary>
        Task LogDrugVerificationFailedAsync(int? operatorId, string? operatorName, int taskId, string? reason, string? ipAddress = null);

        /// <summary>
        /// 记录账号操作日志
        /// </summary>
        Task LogAccountOperationAsync(string operationType, int? operatorId, string? operatorName, int targetAccountId, string? details, string? ipAddress = null);
    }
}
