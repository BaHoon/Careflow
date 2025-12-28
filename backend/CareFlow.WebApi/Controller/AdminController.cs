using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controller;

/// <summary>
/// 管理员控制器
/// 提供系统管理和审计功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly OrderStatusHistoryService _historyService;
    private readonly StaffManagementService _staffService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        OrderStatusHistoryService historyService,
        StaffManagementService staffService,
        ILogger<AdminController> logger)
    {
        _historyService = historyService;
        _staffService = staffService;
        _logger = logger;
    }

    /// <summary>
    /// 查询医嘱状态变更历史
    /// </summary>
    /// <param name="request">查询条件</param>
    /// <returns>分页的历史记录列表</returns>
    [HttpPost("order-status-history")]
    public async Task<IActionResult> QueryOrderStatusHistory([FromBody] QueryOrderStatusHistoryRequestDto request)
    {
        try
        {
            var result = await _historyService.QueryHistoriesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询医嘱状态历史失败");
            return StatusCode(500, new { message = "查询失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 查询人员列表
    /// </summary>
    /// <param name="request">查询条件</param>
    /// <returns>分页的人员列表</returns>
    [HttpPost("staff-list")]
    public async Task<IActionResult> QueryStaffList([FromBody] QueryStaffListRequestDto request)
    {
        try
        {
            var result = await _staffService.QueryStaffListAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询人员列表失败");
            return StatusCode(500, new { message = "查询失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 创建新人员
    /// </summary>
    /// <param name="request">创建人员请求</param>
    /// <returns>创建的人员信息</returns>
    [HttpPost("add-staff")]
    public async Task<IActionResult> AddStaff([FromBody] CreateStaffRequestDto request)
    {
        try
        {
            var result = await _staffService.CreateStaffAsync(request);
            return Ok(new { success = true, data = result, message = "人员创建成功，默认密码为 123456" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "创建人员失败: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建人员失败");
            return StatusCode(500, new { success = false, message = "创建失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 重置人员密码
    /// </summary>
    /// <param name="request">重置密码请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        try
        {
            await _staffService.ResetPasswordAsync(request);
            return Ok(new { success = true, message = "密码重置成功" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "重置密码失败: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置密码失败");
            return StatusCode(500, new { success = false, message = "重置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新员工信息
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的员工信息</returns>
    [HttpPost("update-staff")]
    public async Task<IActionResult> UpdateStaff([FromBody] UpdateStaffRequestDto request)
    {
        try
        {
            var result = await _staffService.UpdateStaffAsync(request);
            return Ok(new { success = true, data = result, message = "员工信息更新成功" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "更新员工失败: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新员工失败");
            return StatusCode(500, new { success = false, message = "更新失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除员工
    /// </summary>
    /// <param name="staffId">员工ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("delete-staff/{staffId}")]
    public async Task<IActionResult> DeleteStaff(string staffId)
    {
        try
        {
            await _staffService.DeleteStaffAsync(staffId);
            return Ok(new { success = true, message = "员工删除成功" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "删除员工失败: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除员工失败");
            return StatusCode(500, new { success = false, message = "删除失败", error = ex.Message });
        }
    }
}
