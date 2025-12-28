using CareFlow.Application.Interfaces;
using CareFlow.Core.Models.Space;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class WardController : ControllerBase
    {
        private readonly ICareFlowDbContext _context;
        private readonly ILogger<WardController> _logger;

        public WardController(
            ICareFlowDbContext context,
            ILogger<WardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取科室下的所有病区
        /// </summary>
        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult> GetWardsByDepartmentId(string departmentId)
        {
            try
            {
                var wards = await _context.Wards
                    .Where(w => w.DepartmentId == departmentId)
                    .OrderBy(w => w.Id)
                    .Select(w => new
                    {
                        Id = w.Id,
                        DepartmentId = w.DepartmentId
                    })
                    .ToListAsync();

                return Ok(wards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取科室病区失败: {DepartmentId}", departmentId);
                return StatusCode(500, new { message = "获取病区列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 创建新病区
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateWard([FromBody] CreateWardDto dto)
        {
            try
            {
                // 检查病区ID是否已存在
                var exists = await _context.Wards.AnyAsync(w => w.Id == dto.Id);
                if (exists)
                {
                    return BadRequest(new { message = $"病区ID {dto.Id} 已存在" });
                }

                // 检查科室是否存在
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
                if (!departmentExists)
                {
                    return BadRequest(new { message = $"科室 {dto.DepartmentId} 不存在" });
                }

                var ward = new Ward
                {
                    Id = dto.Id,
                    DepartmentId = dto.DepartmentId
                };

                _context.Wards.Add(ward);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ 病区创建成功: {WardId}", dto.Id);

                return Ok(new
                {
                    Id = ward.Id,
                    DepartmentId = ward.DepartmentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建病区失败");
                return StatusCode(500, new { message = "创建病区失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 删除病区（仅当病区下没有病床时允许删除）
        /// </summary>
        [HttpDelete("{wardId}")]
        public async Task<ActionResult> DeleteWard(string wardId)
        {
            try
            {
                var ward = await _context.Wards
                    .Include(w => w.Beds)
                    .FirstOrDefaultAsync(w => w.Id == wardId);

                if (ward == null)
                {
                    return NotFound(new { message = $"病区 {wardId} 不存在" });
                }

                // 检查病区下是否有病床
                if (ward.Beds != null && ward.Beds.Any())
                {
                    return BadRequest(new { message = $"病区 {wardId} 下还有 {ward.Beds.Count} 张病床，无法删除" });
                }

                _context.Wards.Remove(ward);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ 病区删除成功: {WardId}", wardId);

                return Ok(new { message = "删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除病区失败: {WardId}", wardId);
                return StatusCode(500, new { message = "删除病区失败", error = ex.Message });
            }
        }
    }

    public class CreateWardDto
    {
        public string Id { get; set; } = null!;
        public string DepartmentId { get; set; } = null!;
    }
}
