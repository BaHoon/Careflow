using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; // 引用你新定义的 DTO
using CareFlow.Application.Services.Nursing; // 引用 Service
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NursingController : ControllerBase
    {
        private readonly IVitalSignService _vitalSignService;
        private readonly NursingTaskGenerator _taskGenerator;

        // 构造函数注入两个服务
        public NursingController(
            IVitalSignService vitalSignService, 
            NursingTaskGenerator taskGenerator)
        {
            _vitalSignService = vitalSignService;
            _taskGenerator = taskGenerator;
        }

        /// <summary>
        /// [管理端/定时任务] 生成今日护理任务
        /// </summary>
        /// <param name="deptId">科室ID (如 DEPT001)</param>
        /// <returns></returns>
        [HttpPost("tasks/generate")]
        public async Task<IActionResult> GenerateDailyTasks(string deptId)
        {
            try
            {
                // 生成今天的任务
                var today = DateOnly.FromDateTime(DateTime.Now);
                
                await _taskGenerator.GenerateDailyTasksAsync(deptId, today);
                
                return Ok(new { message = $"科室 {deptId} 的 {today} 护理任务已生成" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "生成任务失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 提交体征数据并完成任务
        /// </summary>
        /// <param name="dto">提交的数据包</param>
        /// <returns></returns>
        [HttpPost("tasks/submit")]
        public async Task<IActionResult> SubmitVitalSigns([FromBody] NursingTaskSubmissionDto dto)
        {
            if (dto == null) return BadRequest("提交数据不能为空");

            try
            {
                await _vitalSignService.SubmitVitalSignsAsync(dto);
                return Ok(new { message = "执行成功，数据已录入，任务状态已更新" });
            }
            catch (Exception ex)
            {
                // 生产环境建议记录日志
                return StatusCode(500, new { message = "提交失败", error = ex.Message });
            }
        }
    }
}