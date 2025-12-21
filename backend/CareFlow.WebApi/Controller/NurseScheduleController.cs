using CareFlow.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 护士排班控制器
/// </summary>
[ApiController]
[Route("api/nurse/schedule")]
public class NurseScheduleController : ControllerBase
{
    private readonly INurseScheduleRepository _scheduleRepository;
    private readonly ILogger<NurseScheduleController> _logger;

    public NurseScheduleController(
        INurseScheduleRepository scheduleRepository,
        ILogger<NurseScheduleController> logger)
    {
        _scheduleRepository = scheduleRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取护士当前排班的病区
    /// </summary>
    /// <param name="nurseId">护士ID</param>
    /// <returns>当前排班的病区ID，如果没有排班返回null</returns>
    [HttpGet("current-ward/{nurseId}")]
    public async Task<ActionResult<CurrentWardDto>> GetCurrentWard(string nurseId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nurseId))
            {
                return BadRequest(new { message = "护士ID不能为空" });
            }

            _logger.LogInformation("查询护士 {NurseId} 当前排班病区", nurseId);

            var currentTime = DateTime.Now;
            
            // 查询护士在各病区的排班记录
            var rosters = await _scheduleRepository.ListAsync(r => 
                r.StaffId == nurseId && 
                r.WorkDate == DateOnly.FromDateTime(currentTime) &&
                r.Status == "Scheduled");

            if (rosters == null || rosters.Count == 0)
            {
                _logger.LogInformation("护士 {NurseId} 今日无排班记录", nurseId);
                return Ok(new CurrentWardDto { WardId = null, WardName = null });
            }

            // 如果有多个排班，取第一个（实际业务中护士通常只在一个病区值班）
            var currentRoster = rosters.First();
            
            _logger.LogInformation("✅ 护士 {NurseId} 当前在病区 {WardId} 值班", 
                nurseId, currentRoster.WardId);

            return Ok(new CurrentWardDto 
            { 
                WardId = currentRoster.WardId,
                WardName = currentRoster.WardId // Ward只有Id没有Name，使用Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询护士当前排班病区失败");
            return StatusCode(500, new { message = "查询失败", error = ex.Message });
        }
    }
}

/// <summary>
/// 当前排班病区DTO
/// </summary>
public class CurrentWardDto
{
    public string? WardId { get; set; }
    public string? WardName { get; set; }
}
