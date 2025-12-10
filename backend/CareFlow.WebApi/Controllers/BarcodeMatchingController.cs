using CareFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarcodeMatchingController : ControllerBase
{
    private readonly IBarcodeMatchingService _barcodeMatchingService;
    private readonly ILogger<BarcodeMatchingController> _logger;

    public BarcodeMatchingController(
        IBarcodeMatchingService barcodeMatchingService,
        ILogger<BarcodeMatchingController> logger)
    {
        _barcodeMatchingService = barcodeMatchingService;
        _logger = logger;
    }

    /// <summary>
    /// 验证执行任务和患者条形码匹配
    /// </summary>
    /// <param name="executionTaskBarcode">执行任务条形码图片文件</param>
    /// <param name="patientBarcode">患者条形码图片文件</param>
    /// <param name="toleranceMinutes">允许的时间偏差分钟数（默认30分钟）</param>
    /// <returns>匹配验证结果</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<BarcodeMatchingResult>> ValidateBarcodeMatch(
        [Required] IFormFile executionTaskBarcode,
        [Required] IFormFile patientBarcode,
        [FromForm] int toleranceMinutes = 30)
    {
        try
        {
            // 输入验证
            if (executionTaskBarcode == null || executionTaskBarcode.Length == 0)
            {
                return BadRequest("执行任务条形码图片不能为空");
            }

            if (patientBarcode == null || patientBarcode.Length == 0)
            {
                return BadRequest("患者条形码图片不能为空");
            }

            if (toleranceMinutes < 0 || toleranceMinutes > 1440) // 最大24小时
            {
                return BadRequest("时间偏差范围必须在0-1440分钟之间");
            }

            // 验证文件类型
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            var taskBarcodeExtension = Path.GetExtension(executionTaskBarcode.FileName).ToLowerInvariant();
            var patientBarcodeExtension = Path.GetExtension(patientBarcode.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(taskBarcodeExtension))
            {
                return BadRequest($"执行任务条形码图片格式不支持: {taskBarcodeExtension}");
            }

            if (!allowedExtensions.Contains(patientBarcodeExtension))
            {
                return BadRequest($"患者条形码图片格式不支持: {patientBarcodeExtension}");
            }

            // 验证文件大小（最大10MB）
            const long maxFileSize = 10 * 1024 * 1024;
            if (executionTaskBarcode.Length > maxFileSize)
            {
                return BadRequest($"执行任务条形码图片文件过大: {executionTaskBarcode.Length / 1024 / 1024}MB，最大允许10MB");
            }

            if (patientBarcode.Length > maxFileSize)
            {
                return BadRequest($"患者条形码图片文件过大: {patientBarcode.Length / 1024 / 1024}MB，最大允许10MB");
            }

            _logger.LogInformation("开始处理条形码匹配验证请求: 执行任务条形码={TaskBarcodeFile}, 患者条形码={PatientBarcodeFile}, 时间偏差={ToleranceMinutes}分钟",
                executionTaskBarcode.FileName, patientBarcode.FileName, toleranceMinutes);

            // 调用服务进行验证
            using var taskBarcodeStream = executionTaskBarcode.OpenReadStream();
            using var patientBarcodeStream = patientBarcode.OpenReadStream();

            var result = await _barcodeMatchingService.ValidateBarcodeMatchAsync(
                taskBarcodeStream, 
                patientBarcodeStream, 
                toleranceMinutes);

            _logger.LogInformation("条形码匹配验证完成: 结果={IsMatched}, 执行任务ID={TaskId}, 患者ID={PatientId}",
                result.IsMatched, result.ExecutionTaskId, result.PatientId);

            // 根据匹配结果返回相应的HTTP状态码
            if (result.IsMatched)
            {
                return Ok(result);
            }
            else
            {
                // 匹配失败，但不是服务器错误，返回200状态码但IsMatched=false
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "条形码匹配验证API调用失败");
            return StatusCode(500, new BarcodeMatchingResult
            {
                IsMatched = false,
                ErrorMessage = "服务器内部错误，请稍后重试",
                ScanTime = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 获取API使用说明
    /// </summary>
    [HttpGet("help")]
    public ActionResult<object> GetHelp()
    {
        return Ok(new
        {
            Description = "条形码匹配验证API",
            Endpoints = new[]
            {
                new
                {
                    Method = "POST",
                    Path = "/api/barcodematching/validate",
                    Description = "验证执行任务和患者条形码匹配",
                    Parameters = new
                    {
                        executionTaskBarcode = "执行任务条形码图片文件（支持png、jpg、jpeg、bmp、gif格式，最大10MB）",
                        patientBarcode = "患者条形码图片文件（支持png、jpg、jpeg、bmp、gif格式，最大10MB）",
                        toleranceMinutes = "允许的时间偏差分钟数（可选，默认30分钟，范围0-1440分钟）"
                    },
                    Response = new
                    {
                        IsMatched = "bool - 是否匹配成功",
                        ExecutionTaskId = "long - 执行任务ID",
                        PatientId = "string - 患者ID",
                        PlannedStartTime = "DateTime - 计划执行时间",
                        ScanTime = "DateTime - 扫码时间",
                        TimeDifferenceMinutes = "double - 时间差异（分钟）",
                        ValidationDetails = "object - 详细验证信息",
                        ErrorMessage = "string - 错误消息（如果匹配失败）"
                    }
                }
            },
            ExampleUsage = new
            {
                CurlCommand = "curl -X POST \"/api/barcodematching/validate\" -H \"Content-Type: multipart/form-data\" -F \"executionTaskBarcode=@task_barcode.png\" -F \"patientBarcode=@patient_barcode.png\" -F \"toleranceMinutes=30\""
            }
        });
    }
}