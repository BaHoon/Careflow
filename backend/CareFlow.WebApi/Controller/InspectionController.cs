using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;
    private readonly IRepository<ExecutionTask, long> _taskRepo;

    public InspectionController(
        IInspectionService inspectionService,
        IRepository<ExecutionTask, long> taskRepo)
    {
        _inspectionService = inspectionService;
        _taskRepo = taskRepo;
    }

    /// <summary>
    /// 创建检查医嘱
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> CreateInspectionOrder([FromBody] CreateInspectionOrderDto dto)
    {
        try
        {
            var orderId = await _inspectionService.CreateInspectionOrderAsync(dto);
            return Ok(new { orderId, message = "检查医嘱创建成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 更新预约信息 (模拟接收 RIS/LIS 的预约反馈)
    /// </summary>
    [HttpPut("orders/appointment")]
    public async Task<IActionResult> UpdateAppointment([FromBody] UpdateAppointmentDto dto)
    {
        try
        {
            await _inspectionService.UpdateAppointmentAsync(dto);
            return Ok(new { message = "预约信息更新成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 更新检查状态
    /// </summary>
    [HttpPut("orders/status")]
    public async Task<IActionResult> UpdateInspectionStatus([FromBody] UpdateInspectionStatusDto dto)
    {
        try
        {
            await _inspectionService.UpdateInspectionStatusAsync(dto);
            return Ok(new { message = "检查状态更新成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取检查医嘱详情
    /// </summary>
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetInspectionOrderDetail(long orderId)
    {
        try
        {
            var detail = await _inspectionService.GetInspectionOrderDetailAsync(orderId);
            return Ok(detail);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 生成检查导引单
    /// </summary>
    [HttpGet("orders/{orderId}/guide")]
    public async Task<IActionResult> GenerateInspectionGuide(long orderId)
    {
        try
        {
            var guide = await _inspectionService.GenerateInspectionGuideAsync(orderId);
            return Ok(guide);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取患者的所有检查医嘱
    /// </summary>
    [HttpGet("orders/patient/{patientId}")]
    public async Task<IActionResult> GetPatientInspectionOrders(string patientId)
    {
        try
        {
            var orders = await _inspectionService.GetPatientInspectionOrdersAsync(patientId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取护士看板数据 (按病区查询)
    /// </summary>
    [HttpGet("nurse-board/{wardId}")]
    public async Task<IActionResult> GetNurseBoardData(string wardId)
    {
        try
        {
            var data = await _inspectionService.GetNurseBoardDataAsync(wardId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 创建检查报告 (模拟从 RIS/LIS 接收报告)
    /// </summary>
    [HttpPost("reports")]
    public async Task<IActionResult> CreateInspectionReport([FromBody] CreateInspectionReportDto dto)
    {
        try
        {
            var reportId = await _inspectionService.CreateInspectionReportAsync(dto);
            return Ok(new { reportId, message = "检查报告创建成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取检查报告详情
    /// </summary>
    [HttpGet("reports/{reportId}")]
    public async Task<IActionResult> GetInspectionReportDetail(long reportId)
    {
        try
        {
            var detail = await _inspectionService.GetInspectionReportDetailAsync(reportId);
            return Ok(detail);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 根据医嘱ID获取报告列表
    /// </summary>
    [HttpGet("reports/order/{orderId}")]
    public async Task<IActionResult> GetReportsByOrderId(long orderId)
    {
        try
        {
            var reports = await _inspectionService.GetReportsByOrderIdAsync(orderId);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 更新报告状态
    /// </summary>
    [HttpPut("reports/status")]
    public async Task<IActionResult> UpdateReportStatus([FromBody] UpdateReportStatusDto dto)
    {
        try
        {
            await _inspectionService.UpdateReportStatusAsync(dto);
            return Ok(new { message = "报告状态更新成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ===== 扫码接口 =====

    /// <summary>
    /// 步骤5：检查站扫码 - 患者签到（先扫检查单码，再扫患者手环码）
    /// </summary>
    [HttpPost("scan/check-in")]
    public async Task<IActionResult> ScanCheckIn([FromBody] DualScanDto dto)
    {
        try
        {
            // 1. 验证检查单二维码（任务码）
            var task = await _taskRepo.GetQueryable()
                .Include(t => t.MedicalOrder)
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);
            
            if (task == null)
                return BadRequest(new { message = "检查单无效，请检查二维码" });
            
            if (!task.DataPayload.Contains("INSP_CHECKIN"))
                return BadRequest(new { message = "二维码类型错误，这不是签到码" });
            
            if (task.Status != ExecutionTaskStatus.Pending.ToString())
                return BadRequest(new { message = $"任务状态异常：{task.Status}，可能已签到" });

            // 2. 验证患者手环二维码
            if (task.PatientId != dto.PatientId)
                return BadRequest(new { message = "患者信息不匹配！请核对患者手环" });

            // 3. 更新医嘱状态为"检查中"
            await _inspectionService.UpdateInspectionStatusAsync(new UpdateInspectionStatusDto
            {
                OrderId = task.MedicalOrderId,
                Status = InspectionOrderStatus.InProgress,
                Timestamp = DateTime.UtcNow
            });

            // 4. 自动完成签到任务
            task.Status = ExecutionTaskStatus.Completed.ToString();
            task.ActualStartTime = DateTime.UtcNow;
            task.ActualEndTime = DateTime.UtcNow;
            task.ExecutorStaffId = dto.NurseId;
            await _taskRepo.UpdateAsync(task);

            return Ok(new 
            { 
                message = "✅ 签到成功！患者已开始检查",
                orderId = task.MedicalOrderId,
                patientId = task.PatientId,
                status = "检查中"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"签到失败：{ex.Message}" });
        }
    }

    /// <summary>
    /// 步骤6：检查站扫码 - 检查完成（扫患者手环码）
    /// </summary>
    [HttpPost("scan/check-complete")]
    public async Task<IActionResult> ScanCheckComplete([FromBody] CompleteScanDto dto)
    {
        try
        {
            // 1. 查找该患者未完成的检查完成任务
            var task = await _taskRepo.GetQueryable()
                .Include(t => t.MedicalOrder)
                .Where(t => t.PatientId == dto.PatientId)
                .Where(t => t.DataPayload.Contains("INSP_COMPLETE"))
                .Where(t => t.Status == ExecutionTaskStatus.Pending.ToString())
                .OrderBy(t => t.PlannedStartTime)
                .FirstOrDefaultAsync();
            
            if (task == null)
                return BadRequest(new { message = "未找到该患者的待完成检查任务" });

            // 2. 更新医嘱状态为"已回病房"
            await _inspectionService.UpdateInspectionStatusAsync(new UpdateInspectionStatusDto
            {
                OrderId = task.MedicalOrderId,
                Status = InspectionOrderStatus.BackToWard,
                Timestamp = DateTime.UtcNow
            });

            // 3. 自动完成检查完成任务
            task.Status = ExecutionTaskStatus.Completed.ToString();
            task.ActualStartTime = DateTime.UtcNow;
            task.ActualEndTime = DateTime.UtcNow;
            task.ExecutorStaffId = dto.NurseId;
            await _taskRepo.UpdateAsync(task);

            return Ok(new 
            { 
                message = "✅ 检查完成确认成功！患者可返回病房",
                orderId = task.MedicalOrderId,
                patientId = task.PatientId,
                status = "已回病房"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"完成确认失败：{ex.Message}" });
        }
    }
}

/// <summary>
/// 双码扫描 DTO（用于签到）
/// </summary>
public class DualScanDto
{
    /// <summary>
    /// 任务ID（从检查单二维码解析）
    /// </summary>
    public long TaskId { get; set; }
    
    /// <summary>
    /// 患者ID（从患者手环二维码解析）
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// 扫码护士ID
    /// </summary>
    public string NurseId { get; set; } = string.Empty;
}

/// <summary>
/// 完成扫描 DTO（用于检查完成）
/// </summary>
public class CompleteScanDto
{
    /// <summary>
    /// 患者ID（从患者手环二维码解析）
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// 扫码护士ID
    /// </summary>
    public string NurseId { get; set; } = string.Empty;
}
