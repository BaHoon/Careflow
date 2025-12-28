using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentManagementService _departmentService;
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<DepartmentController> _logger;

        public DepartmentController(
            IDepartmentManagementService departmentService,
            ISystemLogService systemLogService,
            ILogger<DepartmentController> logger)
        {
            _departmentService = departmentService;
            _systemLogService = systemLogService;
            _logger = logger;
        }

        /// <summary>
        /// 获取科室列表
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetDepartmentList([FromQuery] QueryDepartmentRequestDto request)
        {
            try
            {
                var result = await _departmentService.GetDepartmentListAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取科室列表失败");
                return StatusCode(500, new { message = "获取科室列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取科室详情
        /// </summary>
        [HttpGet("{departmentId}")]
        public async Task<IActionResult> GetDepartment(string departmentId)
        {
            try
            {
                var department = await _departmentService.GetDepartmentByIdAsync(departmentId);
                if (department == null)
                {
                    return NotFound(new { message = "科室不存在" });
                }
                return Ok(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取科室详情失败");
                return StatusCode(500, new { message = "获取科室详情失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 创建科室
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] SaveDepartmentDto dto)
        {
            try
            {
                var operatorId = User.FindFirst("id")?.Value;
                var operatorName = User.FindFirst("name")?.Value;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                var department = await _departmentService.CreateDepartmentAsync(dto, operatorId, operatorName, ipAddress);

                return Ok(department);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建科室失败");
                return StatusCode(500, new { message = "创建科室失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 更新科室
        /// </summary>
        [HttpPut("{departmentId}")]
        public async Task<IActionResult> UpdateDepartment(string departmentId, [FromBody] SaveDepartmentDto dto)
        {
            try
            {
                var operatorId = User.FindFirst("id")?.Value;
                var operatorName = User.FindFirst("name")?.Value;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                var department = await _departmentService.UpdateDepartmentAsync(departmentId, dto, operatorId, operatorName, ipAddress);

                return Ok(department);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新科室失败");
                return StatusCode(500, new { message = "更新科室失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 删除科室
        /// </summary>
        [HttpDelete("{departmentId}")]
        public async Task<IActionResult> DeleteDepartment(string departmentId)
        {
            try
            {
                var operatorId = User.FindFirst("id")?.Value;
                var operatorName = User.FindFirst("name")?.Value;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                var success = await _departmentService.DeleteDepartmentAsync(departmentId, operatorId, operatorName, ipAddress);
                if (!success)
                {
                    return NotFound(new { message = "科室不存在" });
                }

                return Ok(new { success = true, message = "科室删除成功" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除科室失败");
                return StatusCode(500, new { success = false, message = "删除科室失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有启用的科室（用于下拉选择）
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveDepartments()
        {
            try
            {
                var departments = await _departmentService.GetActiveDepartmentsAsync();
                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取启用科室列表失败");
                return StatusCode(500, new { message = "获取启用科室列表失败", error = ex.Message });
            }
        }
    }
}
