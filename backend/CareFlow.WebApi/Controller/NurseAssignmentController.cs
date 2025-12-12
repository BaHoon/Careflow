using Microsoft.AspNetCore.Mvc;
using CareFlow.Application.Interfaces; // 引用接口

namespace CareFlow.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NurseAssignmentController : ControllerBase
    {
        // 1. 注入你的“计算器”服务
        private readonly INurseAssignmentService _assignmentService;

        public NurseAssignmentController(INurseAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        /// <summary>
        /// 显式测试/预览：计算某患者在当前时间的负责护士
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>护士ID</returns>
        [HttpGet("preview-nurse")]
        public async Task<IActionResult> GetResponsibleNurse(string patientId)
        {
            // 调用服务进行计算
            var nurseId = await _assignmentService.CalculateResponsibleNurseAsync(patientId, DateTime.UtcNow);

            if (nurseId == null)
            {
                return NotFound(new 
                { 
                    Message = "未找到匹配的值班护士", 
                    Reason = "可能原因：该患者无床位、该病区今日无排班、或当前时间非值班时间" 
                });
            }

            return Ok(new 
            { 
                PatientId = patientId, 
                AssignedNurseId = nurseId, 
                QueryTime = DateTime.UtcNow 
            });
        }
    }
}