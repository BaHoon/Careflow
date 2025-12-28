using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Space;
using CareFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 护士排班控制器
/// </summary>
[ApiController]
[Route("api/nurse/schedule")]
public class NurseScheduleController : ControllerBase
{
    private readonly INurseScheduleRepository _scheduleRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NurseScheduleController> _logger;

    public NurseScheduleController(
        INurseScheduleRepository scheduleRepository,
        ApplicationDbContext context,
        ILogger<NurseScheduleController> logger)
    {
        _scheduleRepository = scheduleRepository;
        _context = context;
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

            // 使用UTC时间，与排班表数据一致
            var currentTime = DateTime.UtcNow;
            
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

    /// <summary>
    /// 获取排班列表（支持筛选）
    /// </summary>
    /// <param name="startDate">开始日期（可选）</param>
    /// <param name="endDate">结束日期（可选）</param>
    /// <param name="wardId">病区ID（可选）</param>
    /// <param name="nurseId">护士ID（可选）</param>
    /// <param name="departmentId">科室ID（可选，返回该科室所有病区的排班）</param>
    /// <returns>排班列表</returns>
    [HttpGet("list")]
    public async Task<ActionResult<ScheduleListResponseDto>> GetScheduleList(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? wardId,
        [FromQuery] string? nurseId,
        [FromQuery] string? departmentId)
    {
        try
        {
            _logger.LogInformation("查询排班列表: StartDate={StartDate}, EndDate={EndDate}, WardId={WardId}, NurseId={NurseId}, DepartmentId={DepartmentId}",
                startDate, endDate, wardId, nurseId, departmentId);

            var query = _context.NurseRosters
                .Include(r => r.Nurse)
                .Include(r => r.Ward)
                    .ThenInclude(w => w.Department)
                .Include(r => r.ShiftType)
                .AsQueryable();

            // 日期筛选
            if (startDate.HasValue)
            {
                var start = DateOnly.FromDateTime(startDate.Value);
                query = query.Where(r => r.WorkDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = DateOnly.FromDateTime(endDate.Value);
                query = query.Where(r => r.WorkDate <= end);
            }

            // 科室筛选（优先于病区筛选）
            if (!string.IsNullOrWhiteSpace(departmentId))
            {
                query = query.Where(r => r.Ward.DepartmentId == departmentId);
            }

            // 病区筛选（如果没有指定科室，可以按病区筛选）
            if (!string.IsNullOrWhiteSpace(wardId))
            {
                query = query.Where(r => r.WardId == wardId);
            }

            // 护士筛选
            if (!string.IsNullOrWhiteSpace(nurseId))
            {
                query = query.Where(r => r.StaffId == nurseId);
            }

            var rosters = await query
                .OrderBy(r => r.WorkDate)
                .ThenBy(r => r.WardId)
                .ThenBy(r => r.ShiftType.StartTime)
                .ToListAsync();

            var result = rosters.Select(r => new ScheduleItemDto
            {
                Id = r.Id,
                NurseId = r.StaffId,
                NurseName = r.Nurse?.Name ?? "未知",
                WardId = r.WardId,
                WardName = r.Ward?.Id ?? r.WardId,
                DepartmentName = r.Ward?.Department?.DeptName ?? "未知",
                ShiftId = r.ShiftId,
                ShiftName = r.ShiftType?.ShiftName ?? "未知",
                StartTime = r.ShiftType != null 
                    ? $"{r.ShiftType.StartTime.Hours:D2}:{r.ShiftType.StartTime.Minutes:D2}" 
                    : "",
                EndTime = r.ShiftType != null 
                    ? $"{r.ShiftType.EndTime.Hours:D2}:{r.ShiftType.EndTime.Minutes:D2}" 
                    : "",
                WorkDate = r.WorkDate.ToString("yyyy-MM-dd"),
                Status = r.Status
            }).ToList();

            return Ok(new ScheduleListResponseDto
            {
                Schedules = result,
                TotalCount = result.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询排班列表失败");
            return StatusCode(500, new { message = "查询失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取病区列表（支持按科室筛选）
    /// </summary>
    /// <param name="departmentId">科室ID（可选，返回该科室的所有病区）</param>
    [HttpGet("wards")]
    public async Task<ActionResult<List<WardDto>>> GetWards([FromQuery] string? departmentId = null)
    {
        try
        {
            var query = _context.Wards
                .Include(w => w.Department)
                .AsQueryable();

            // 如果指定了科室，只返回该科室的病区
            if (!string.IsNullOrWhiteSpace(departmentId))
            {
                query = query.Where(w => w.DepartmentId == departmentId);
            }

            var wards = await query
                .Select(w => new WardDto
                {
                    WardId = w.Id,
                    WardName = w.Id,
                    DepartmentId = w.DepartmentId,
                    DepartmentName = w.Department.DeptName
                })
                .OrderBy(w => w.DepartmentId)
                .ThenBy(w => w.WardId)
                .ToListAsync();

            _logger.LogInformation("查询病区列表: DepartmentId={DepartmentId}, 返回数量={Count}",
                departmentId, wards.Count);

            return Ok(wards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询病区列表失败");
            return StatusCode(500, new { message = "查询失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有班次类型列表
    /// </summary>
    [HttpGet("shift-types")]
    public async Task<ActionResult<List<ShiftTypeDto>>> GetShiftTypes()
    {
        try
        {
            var shiftTypesList = await _context.ShiftTypes
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var result = shiftTypesList.Select(s => new ShiftTypeDto
            {
                ShiftId = s.Id,
                ShiftName = s.ShiftName,
                StartTime = $"{s.StartTime.Hours:D2}:{s.StartTime.Minutes:D2}",
                EndTime = $"{s.EndTime.Hours:D2}:{s.EndTime.Minutes:D2}"
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询班次类型列表失败");
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

/// <summary>
/// 排班列表响应DTO
/// </summary>
public class ScheduleListResponseDto
{
    public List<ScheduleItemDto> Schedules { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// 排班项DTO
/// </summary>
public class ScheduleItemDto
{
    public long Id { get; set; }
    public string NurseId { get; set; } = null!;
    public string NurseName { get; set; } = null!;
    public string WardId { get; set; } = null!;
    public string WardName { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
    public string ShiftId { get; set; } = null!;
    public string ShiftName { get; set; } = null!;
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public string WorkDate { get; set; } = null!;
    public string Status { get; set; } = null!;
}

/// <summary>
/// 病区DTO
/// </summary>
public class WardDto
{
    public string WardId { get; set; } = null!;
    public string WardName { get; set; } = null!;
    public string DepartmentId { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
}

/// <summary>
/// 班次类型DTO
/// </summary>
public class ShiftTypeDto
{
    public string ShiftId { get; set; } = null!;
    public string ShiftName { get; set; } = null!;
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
}
