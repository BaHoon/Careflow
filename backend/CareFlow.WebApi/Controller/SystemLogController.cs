using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemLogController : ControllerBase
    {
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<SystemLogController> _logger;

        public SystemLogController(
            ISystemLogService systemLogService,
            ILogger<SystemLogController> logger)
        {
            _systemLogService = systemLogService;
            _logger = logger;
        }

        /// <summary>
        /// 查询系统日志
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSystemLogs([FromQuery] QuerySystemLogRequestDto request)
        {
            try
            {
                _logger.LogInformation("查询系统日志，筛选条件: {@Request}", request);
                var result = await _systemLogService.GetSystemLogsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询系统日志失败");
                return StatusCode(500, new { message = "查询系统日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取操作类型列表（用于筛选）
        /// </summary>
        [HttpGet("operation-types")]
        public IActionResult GetOperationTypes()
        {
            var types = new[]
            {
                new { value = "Login", label = "登录" },
                new { value = "Logout", label = "登出" },
                new { value = "AccountCreated", label = "账号创建" },
                new { value = "AccountModified", label = "账号修改" },
                new { value = "AccountDeleted", label = "账号删除" },
                new { value = "PasswordChanged", label = "密码修改" },
                new { value = "DepartmentCreated", label = "科室创建" },
                new { value = "DepartmentModified", label = "科室修改" },
                new { value = "DepartmentDeleted", label = "科室删除" }
            };

            return Ok(types);
        }

        /// <summary>
        /// 记录登录日志（供登录接口调用）
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> LogLogin([FromBody] LoginLogDto dto)
        {
            try
            {
                await _systemLogService.LogLoginAsync(
                    dto.OperatorId,
                    dto.OperatorName,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    dto.Success,
                    dto.ErrorMessage
                );
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登录日志失败");
                return StatusCode(500, new { message = "记录登录日志失败" });
            }
        }

        /// <summary>
        /// 记录登出日志
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> LogLogout([FromBody] LogoutLogDto dto)
        {
            try
            {
                await _systemLogService.LogLogoutAsync(
                    dto.OperatorId,
                    dto.OperatorName,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登出日志失败");
                return StatusCode(500, new { message = "记录登出日志失败" });
            }
        }
    }

    /// <summary>
    /// 登录日志DTO
    /// </summary>
    public class LoginLogDto
    {
        public int? OperatorId { get; set; }
        public string? OperatorName { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 登出日志DTO
    /// </summary>
    public class LogoutLogDto
    {
        public int? OperatorId { get; set; }
        public string? OperatorName { get; set; }
    }
}
