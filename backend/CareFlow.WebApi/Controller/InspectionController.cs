using CareFlow.Application.DTOs.Inspection;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

/// <summary>
/// 检查医嘱控制器 - 负责检查医嘱的完整工作流
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;
    private readonly IRepository<InspectionOrder, long> _orderRepo;
    private readonly IRepository<ExecutionTask, long> _taskRepo;
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<InspectionController> _logger;

    public InspectionController(
        IInspectionService inspectionService,
        IRepository<InspectionOrder, long> orderRepo,
        IRepository<ExecutionTask, long> taskRepo,
        IBarcodeService barcodeService,
        ILogger<InspectionController> logger)
    {
        _inspectionService = inspectionService;
        _orderRepo = orderRepo;
        _taskRepo = taskRepo;
        _barcodeService = barcodeService;
        _logger = logger;
    }

    /// <summary>
    /// 统一扫码接口(根据任务类型自动处理)
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
    /// 获取检查医嘱列表（支持分页和筛选）
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? inspectionStatus = null,
        [FromQuery] string? ward = null,
        [FromQuery] string? patientName = null)
    {
        try
        {
            var query = _orderRepo.GetQueryable()
                .Include(o => o.Patient)
                    .ThenInclude(p => p.Bed)
                        .ThenInclude(b => b.Ward)
                .AsQueryable();

            // 按检查状态筛选
            if (!string.IsNullOrEmpty(inspectionStatus))
            {
                if (Enum.TryParse<InspectionOrderStatus>(inspectionStatus, out var status))
                {
                    query = query.Where(o => o.InspectionStatus == status);
                }
            }

            // 按病区筛选
            if (!string.IsNullOrEmpty(ward))
            {
                query = query.Where(o => o.Patient.Bed.Ward.Id.Contains(ward));
            }

            // 按患者姓名筛选
            if (!string.IsNullOrEmpty(patientName))
            {
                query = query.Where(o => o.Patient.Name.Contains(patientName));
            }

            // 总数
            var total = await query.CountAsync();

            // 分页
            var orders = await query
                .OrderByDescending(o => o.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    o.PatientId,
                    PatientName = o.Patient.Name,
                    Gender = o.Patient.Gender,
                    Age = o.Patient.Age,
                    BedNumber = o.Patient.Bed.Id,
                    Ward = o.Patient.Bed.Ward.Id,
                    o.ItemCode,
                    o.RisLisId,
                    o.Location,
                    o.Source,
                    InspectionStatus = o.InspectionStatus.ToString(),
                    o.AppointmentTime,
                    o.AppointmentPlace,
                    o.Precautions,
                    o.CheckStartTime,
                    o.CheckEndTime,
                    o.ReportPendingTime,
                    o.ReportTime,
                    o.CreateTime,
                    RequiresAppointment = o.AppointmentTime.HasValue,
                    IsPrinted = false // 需要根据实际打印记录判断
                })
                .ToListAsync();

            return Ok(new
            {
                data = orders,
                total = total,
                pageIndex = pageIndex,
                pageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取检查医嘱列表失败");
            return StatusCode(500, new { message = "获取检查医嘱列表失败", error = ex.Message });
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
    /// 获取检查医嘱关联的执行任务列表
    /// </summary>
    [HttpGet("{orderId}/tasks")]
    public async Task<IActionResult> GetOrderTasks(long orderId)
    {
        try
        {
            var tasks = await _taskRepo.ListAsync(t => t.MedicalOrderId == orderId);
            var taskDtos = tasks.Select(t => new
            {
                t.Id,
                t.MedicalOrderId,
                t.PatientId,
                t.Category,
                t.PlannedStartTime,
                t.ActualStartTime,
                t.ActualEndTime,
                t.Status,
                t.DataPayload
            }).ToList();

            return Ok(new { data = taskDtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取检查医嘱任务列表失败");
            return StatusCode(500, new { message = "获取任务列表失败", error = ex.Message });
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
    
    /// <summary>
    /// 【测试】手动为指定医嘱创建检查报告
    /// </summary>
    [HttpPost("orders/{orderId}/create-report-manually")]
    public async Task<IActionResult> CreateReportManually(long orderId)
    {
        try
        {
            _logger.LogInformation("🧪 手动创建检查报告，医嘱ID: {OrderId}", orderId);
            
            // 1. 获取检查医嘱
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = $"检查医嘱 {orderId} 不存在" });
            }
            
            // 2. 检查是否已经有报告
            if (!string.IsNullOrEmpty(order.ReportId))
            {
                return BadRequest(new { message = $"该医嘱已经有报告了，报告ID: {order.ReportId}" });
            }
            
            // 3. 创建模拟报告
            var reportDto = new CreateInspectionReportDto
            {
                OrderId = orderId,
                RisLisId = order.RisLisId,
                Findings = "[手动测试数据] 检查所见：未见明显异常。",
                Impression = "[手动测试数据] 诊断意见：未见异常。",
                AttachmentUrl = "reports/REPORT.pdf", // 文件路径
                ReviewerId = null, // 不关联审核医生，避免外键约束错误
                ReportSource = order.Source
            };
            
            // 4. 创建报告
            var reportId = await _inspectionService.CreateInspectionReportAsync(reportDto);
            
            _logger.LogInformation("✅ 手动创建报告成功，医嘱ID: {OrderId}, 报告ID: {ReportId}", orderId, reportId);
            
            return Ok(new { 
                success = true,
                reportId, 
                orderId,
                message = "检查报告手动创建成功，可以在前端查看了" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 手动创建报告失败，医嘱ID: {OrderId}", orderId);
            return StatusCode(500, new { message = $"创建报告失败: {ex.Message}" });
        }
    }
}
