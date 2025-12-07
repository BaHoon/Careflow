using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;

    public InspectionController(IInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
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

    /// <summary>
    /// 生成模拟检查医嘱数据
    /// </summary>
    [HttpPost("mock-data")]
    public async Task<IActionResult> GenerateMockData()
    {
        try
        {
            await _inspectionService.GenerateMockInspectionOrdersAsync();
            return Ok(new { message = "模拟数据生成成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
