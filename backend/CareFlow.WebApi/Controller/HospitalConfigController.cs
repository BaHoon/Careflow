using CareFlow.Application.DTOs.HospitalConfig;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HospitalConfigController : ControllerBase
{
    private readonly IRepository<HospitalTimeSlot, int> _timeSlotRepository;
    private readonly ILogger<HospitalConfigController> _logger;

    public HospitalConfigController(
        IRepository<HospitalTimeSlot, int> timeSlotRepository,
        ILogger<HospitalConfigController> logger)
    {
        _timeSlotRepository = timeSlotRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取医院时段配置
    /// </summary>
    [HttpGet("time-slots")]
    public async Task<ActionResult<List<TimeSlotDto>>> GetTimeSlots()
    {
        try
        {
            _logger.LogInformation("获取医院时段配置");

            var timeSlots = await _timeSlotRepository.GetQueryable()
                .OrderBy(t => t.Id)
                .ToListAsync();

            var result = timeSlots.Select(t => new TimeSlotDto
            {
                Id = t.Id,
                SlotCode = t.SlotCode,
                SlotName = t.SlotName,
                DefaultTime = t.DefaultTime.ToString(@"hh\:mm\:ss"),
                Description = $"{t.SlotName}用药时段"
            }).ToList();

            _logger.LogInformation("成功获取 {Count} 个时段配置", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取时段配置失败");
            return StatusCode(500, new { message = "获取时段配置失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 获取给药途径字典
    /// </summary>
    [HttpGet("usage-routes")]
    public ActionResult<List<UsageRouteDto>> GetUsageRoutes()
    {
        try
        {
            _logger.LogInformation("获取给药途径字典");

            // 硬编码给药途径（对应UsageRoute枚举）
            var routes = new List<UsageRouteDto>
            {
                new() { Id = 1, Code = "PO", Name = "口服", Description = "口服给药" },
                new() { Id = 10, Code = "IM", Name = "肌肉注射", Description = "肌肉注射给药" },
                new() { Id = 11, Code = "SC", Name = "皮下注射", Description = "皮下注射给药" },
                new() { Id = 12, Code = "ID", Name = "皮内注射", Description = "皮内注射给药" },
                new() { Id = 20, Code = "IV_DRIP", Name = "静脉滴注", Description = "静脉滴注给药" },
                new() { Id = 21, Code = "IV_PUSH", Name = "静脉推注", Description = "静脉推注给药" }
            };

            _logger.LogInformation("成功获取 {Count} 个给药途径", routes.Count);
            return Ok(routes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取给药途径失败");
            return StatusCode(500, new { message = "获取给药途径失败: " + ex.Message });
        }
    }
}
