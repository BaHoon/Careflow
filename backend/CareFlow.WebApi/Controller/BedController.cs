using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class BedController : ControllerBase
    {
        private readonly IBedService _bedService;
        private readonly ILogger<BedController> _logger;

        public BedController(
            IBedService bedService,
            ILogger<BedController> logger)
        {
            _bedService = bedService;
            _logger = logger;
        }

        /// <summary>
        /// 获取科室下的所有病床
        /// </summary>
        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<List<BedDto>>> GetBedsByDepartmentId(string departmentId)
        {
            try
            {
                var beds = await _bedService.GetBedsByDepartmentIdAsync(departmentId);
                return Ok(beds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取科室病床失败: {DepartmentId}", departmentId);
                return StatusCode(500, new { message = "获取病床列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 创建新病床
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BedDto>> CreateBed([FromBody] CreateBedDto dto)
        {
            try
            {
                var bed = await _bedService.CreateBedAsync(dto);
                return Ok(bed);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建病床失败");
                return StatusCode(500, new { message = "创建病床失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 删除病床
        /// </summary>
        [HttpDelete("{bedId}")]
        public async Task<ActionResult> DeleteBed(string bedId)
        {
            try
            {
                var success = await _bedService.DeleteBedAsync(bedId);
                if (!success)
                {
                    return NotFound(new { message = $"病床 {bedId} 不存在" });
                }
                return Ok(new { message = "病床删除成功" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除病床失败: {BedId}", bedId);
                return StatusCode(500, new { message = "删除病床失败", error = ex.Message });
            }
        }
    }
}
