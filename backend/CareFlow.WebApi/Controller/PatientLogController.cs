using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Patient;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 患者日志API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PatientLogController : ControllerBase
{
    private readonly IPatientLogService _logService;

    public PatientLogController(IPatientLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 【患者日志】获取患者日志时间线数据
    /// </summary>
    /// <param name="patientId">患者ID (必填)</param>
    /// <param name="startDate">开始日期 (可选，默认前天00:00:00)</param>
    /// <param name="endDate">结束日期 (可选，默认今天23:59:59)</param>
    /// <param name="contentTypes">内容类型 (可选，多选逗号分隔: MedicalOrders,NursingRecords,ExamReports，默认全选)</param>
    /// <returns>患者日志数据</returns>
    /// <response code="200">成功返回患者日志数据</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="404">患者不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [ProducesResponseType(typeof(PatientLogResponseDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetPatientLog(
        [FromQuery] string patientId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? contentTypes = null)
    {
        try
        {
            // 验证必填参数
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return BadRequest(new { message = "患者ID不能为空" });
            }

            // 计算默认时间范围：前天00:00:00 ~ 今天23:59:59 (UTC时间)
            // 🔧 使用UTC时间，与数据库保持一致，避免PostgreSQL时区问题
            var now = DateTime.UtcNow;
            var defaultStartDate = now.Date.AddDays(-2); // 前天00:00:00 UTC
            var defaultEndDate = now.Date.AddDays(1).AddSeconds(-1); // 今天23:59:59 UTC

            // 构建查询DTO
            var query = new PatientLogQueryDto
            {
                PatientId = patientId,
                StartDate = startDate ?? defaultStartDate,
                EndDate = endDate ?? defaultEndDate,
                ContentTypes = string.IsNullOrWhiteSpace(contentTypes)
                    ? new List<string> { "MedicalOrders", "NursingRecords", "ExamReports" }
                    : contentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim())
                                 .ToList()
            };

            // 验证内容类型
            var validTypes = new HashSet<string> { "MedicalOrders", "NursingRecords", "ExamReports" };
            var invalidTypes = query.ContentTypes.Where(t => !validTypes.Contains(t)).ToList();
            if (invalidTypes.Any())
            {
                return BadRequest(new 
                { 
                    message = "无效的内容类型",
                    invalidTypes = invalidTypes,
                    validTypes = new[] { "MedicalOrders", "NursingRecords", "ExamReports" }
                });
            }

            // 验证日期范围
            if (query.EndDate < query.StartDate)
            {
                return BadRequest(new { message = "结束日期不能早于开始日期" });
            }

            // 调用服务获取数据
            var result = await _logService.GetPatientLogAsync(query);

            return Ok(result);
        }
        catch (Exception ex) when (ex.Message.Contains("未找到"))
        {
            // 患者不存在
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // 记录错误日志 (生产环境应使用专业日志框架)
            Console.WriteLine($"❌ 患者日志查询失败: {ex.Message}");
            Console.WriteLine($"   堆栈跟踪: {ex.StackTrace}");

            return StatusCode(500, new 
            { 
                message = "查询患者日志失败",
                error = ex.Message
            });
        }
    }
}
