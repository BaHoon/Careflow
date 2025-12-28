using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.DTOs.Common;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services.Admin
{
    public class SystemLogService : ISystemLogService
    {
        private readonly ICareFlowDbContext _context;
        private readonly ILogger<SystemLogService> _logger;

        public SystemLogService(
            ICareFlowDbContext context,
            ILogger<SystemLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(CreateSystemLogDto dto)
        {
            try
            {
                var log = new SystemLog
                {
                    OperationType = dto.OperationType,
                    OperatorId = dto.OperatorId,
                    OperatorName = dto.OperatorName,
                    OperationTime = DateTime.UtcNow,
                    OperationDetails = dto.OperationDetails,
                    IpAddress = dto.IpAddress,
                    Result = dto.Result,
                    ErrorMessage = dto.ErrorMessage,
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId
                };

                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ 系统日志记录成功: {OperationType} by {OperatorName}", dto.OperationType, dto.OperatorName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 记录系统日志失败");
                // 日志记录失败不应该影响主业务流程，所以这里只记录错误
            }
        }

        public async Task<PagedResultDto<SystemLogDto>> GetSystemLogsAsync(QuerySystemLogRequestDto request)
        {
            _logger.LogInformation("查询系统日志，筛选条件: {@Request}", request);

            var query = _context.SystemLogs.AsQueryable();

            // 操作类型筛选
            if (!string.IsNullOrEmpty(request.OperationType))
            {
                query = query.Where(l => l.OperationType == request.OperationType);
            }

            // 操作人姓名筛选
            if (!string.IsNullOrEmpty(request.OperatorName))
            {
                query = query.Where(l => l.OperatorName != null && l.OperatorName.Contains(request.OperatorName));
            }

            // 时间范围筛选
            if (request.StartTime.HasValue)
            {
                query = query.Where(l => l.OperationTime >= request.StartTime.Value);
            }
            if (request.EndTime.HasValue)
            {
                query = query.Where(l => l.OperationTime <= request.EndTime.Value);
            }

            // 操作结果筛选
            if (!string.IsNullOrEmpty(request.Result))
            {
                query = query.Where(l => l.Result == request.Result);
            }

            // 总数
            var total = await query.CountAsync();

            // 分页和排序（最新的在前）
            var logs = await query
                .OrderByDescending(l => l.OperationTime)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = logs.Select(l => new SystemLogDto
            {
                LogId = l.LogId,
                OperationType = l.OperationType,
                OperatorId = l.OperatorId,
                OperatorName = l.OperatorName,
                OperationTime = l.OperationTime,
                OperationDetails = l.OperationDetails,
                IpAddress = l.IpAddress,
                Result = l.Result,
                ErrorMessage = l.ErrorMessage,
                EntityType = l.EntityType,
                EntityId = l.EntityId
            }).ToList();

            _logger.LogInformation("✅ 成功获取 {Count} 条系统日志", items.Count);

            return new PagedResultDto<SystemLogDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        public async Task LogLoginAsync(int? operatorId, string? operatorName, string? ipAddress, bool success, string? errorMessage = null)
        {
            await LogAsync(new CreateSystemLogDto
            {
                OperationType = "Login",
                OperatorId = operatorId,
                OperatorName = operatorName,
                OperationDetails = success ? $"{operatorName} 登录成功" : $"{operatorName} 登录失败",
                IpAddress = ipAddress,
                Result = success ? "Success" : "Failed",
                ErrorMessage = errorMessage
            });
        }

        public async Task LogLogoutAsync(int? operatorId, string? operatorName, string? ipAddress)
        {
            await LogAsync(new CreateSystemLogDto
            {
                OperationType = "Logout",
                OperatorId = operatorId,
                OperatorName = operatorName,
                OperationDetails = "用户登出",
                IpAddress = ipAddress,
                Result = "Success"
            });
        }

        public async Task LogOrderStopAsync(int? operatorId, string? operatorName, int orderId, string? reason, string? ipAddress = null)
        {
            await LogAsync(new CreateSystemLogDto
            {
                OperationType = "OrderStop",
                OperatorId = operatorId,
                OperatorName = operatorName,
                OperationDetails = $"停止医嘱，原因: {reason}",
                IpAddress = ipAddress,
                Result = "Success",
                EntityType = "MedicalOrder",
                EntityId = orderId
            });
        }

        public async Task LogDrugVerificationFailedAsync(int? operatorId, string? operatorName, int taskId, string? reason, string? ipAddress = null)
        {
            await LogAsync(new CreateSystemLogDto
            {
                OperationType = "DrugVerificationFailed",
                OperatorId = operatorId,
                OperatorName = operatorName,
                OperationDetails = $"药品核对失败，原因: {reason}",
                IpAddress = ipAddress,
                Result = "Failed",
                EntityType = "ExecutionTask",
                EntityId = taskId
            });
        }

        public async Task LogAccountOperationAsync(string operationType, int? operatorId, string? operatorName, int targetAccountId, string? details, string? ipAddress = null)
        {
            await LogAsync(new CreateSystemLogDto
            {
                OperationType = operationType,
                OperatorId = operatorId,
                OperatorName = operatorName,
                OperationDetails = details,
                IpAddress = ipAddress,
                Result = "Success",
                EntityType = "Staff",
                EntityId = targetAccountId
            });
        }
    }
}
