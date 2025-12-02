using CareFlow.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminStaffController : ControllerBase
{
    private readonly StaffService _staffService;

    public AdminStaffController(StaffService staffService)
    {
        _staffService = staffService;
    }

    // GET: api/AdminStaff
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try 
        {
            var list = await _staffService.GetAllStaffAsync();
            return Ok(list);
        }
        catch (Exception ex)
        {
            // 实际项目建议用 Logger 记录日志
            return StatusCode(500, new { message = "服务器内部错误", details = ex.Message });
        }
    }
}