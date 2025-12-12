using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Nursing;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 检查医嘱控制器 - 负责检查医嘱的完整工作流
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;
    private readonly IRepository<ExecutionTask, long> _taskRepo;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<InspectionController> _logger;

    public InspectionController(
        IInspectionService inspectionService,
        IRepository<ExecutionTask, long> taskRepo,
        IBarcodeService barcodeService,
        ILogger<InspectionController> logger)
    {
        _inspectionService = inspectionService;
        _taskRepo = taskRepo;
        _barcodeService = barcodeService;
        _logger = logger;
    }

    /// <summary>
    /// 步骤1：病房护士发送检查申请到检查站
    /// </summary>
    [HttpPost("send-request")]
    public async Task<IActionResult> SendRequest([FromBody] SendInspectionRequestDto dto)
    {
        try
        {
            await _inspectionService.SendInspectionRequestAsync(dto);
            return Ok(new { message = "检查申请已发送到检查站" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 步骤2：检查站护士查看待处理的检查申请列表
    /// </summary>
    [HttpGet("pending-requests")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] string inspectionStationId)
    {
        try
        {
            var requests = await _inspectionService.GetPendingRequestsAsync(inspectionStationId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 步骤3：检查站护士创建预约(自动生成3个执行任务)
    /// </summary>
    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            await _inspectionService.CreateAppointmentAsync(dto);
            return Ok(new { message = "预约创建成功，已生成3个执行任务(打印导引单、签到、等待报告)" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 步骤4/5/6：统一扫码接口(根据任务类型自动处理)
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] SingleScanDto dto)
    {
        try
        {
            var result = await _inspectionService.ProcessScanAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取检查医嘱详情
    /// </summary>
    [HttpGet("detail/{orderId}")]
    public async Task<IActionResult> GetDetail(long orderId)
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
    /// 步骤7：检查站上传检查报告(自动完成等待报告任务)
    /// </summary>
    [HttpPost("reports")]
    public async Task<IActionResult> CreateReport([FromBody] CreateInspectionReportDto dto)
    {
        try
        {
            var reportId = await _inspectionService.CreateInspectionReportAsync(dto);
            return Ok(new { reportId, message = "检查报告创建成功，等待报告任务已自动完成" });
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
    public async Task<IActionResult> GetReport(long reportId)
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
    /// 获取检查任务的条形码图片
    /// </summary>
    [HttpGet("task/{taskId}/barcode")]
    public async Task<IActionResult> GetTaskBarcode(long taskId)
    {
        try
        {
            _logger.LogInformation("开始获取检查任务 {TaskId} 的条形码", taskId);

            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task == null)
            {
                return NotFound(new { Success = false, Message = $"未找到ID为 {taskId} 的执行任务" });
            }

            var barcodeIndex = new BarcodeIndex
            {
                TableName = "ExecutionTasks",
                RecordId = taskId.ToString()
            };

            var imageBytes = await _barcodeService.GenerateBarcodeAsync(barcodeIndex);

            _logger.LogInformation("成功生成检查任务 {TaskId} 的条形码", taskId);
            return File(imageBytes, "image/png", $"InspectionTask-{taskId}-barcode.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取检查任务 {TaskId} 的条形码时发生错误", taskId);
            return BadRequest(new { Success = false, Message = $"获取条形码失败: {ex.Message}" });
        }
    }
}
