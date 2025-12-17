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
    /// <param name="checkTime">是否检查时间（默认true）</param>
    /// <returns>匹配验证结果</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<BarcodeMatchingResult>> ValidateBarcodeMatch(
        [Required] IFormFile executionTaskBarcode,
        [Required] IFormFile patientBarcode,
        [FromForm] int toleranceMinutes = 30,
        [FromForm] bool checkTime = true)
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

            _logger.LogInformation("开始处理条形码匹配验证请求: 执行任务条形码={TaskBarcodeFile}, 患者条形码={PatientBarcodeFile}, 时间偏差={ToleranceMinutes}分钟, 检查时间={CheckTime}",
                executionTaskBarcode.FileName, patientBarcode.FileName, toleranceMinutes, checkTime);

            // 调用服务进行验证
            using var taskBarcodeStream = executionTaskBarcode.OpenReadStream();
            using var patientBarcodeStream = patientBarcode.OpenReadStream();

            var result = await _barcodeMatchingService.ValidateBarcodeMatchAsync(
                taskBarcodeStream, 
                patientBarcodeStream, 
                toleranceMinutes,
                checkTime);

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
    /// 验证执行任务和药品条形码匹配
    /// </summary>
    /// <param name="executionTaskBarcode">执行任务条形码图片文件</param>
    /// <param name="drugBarcode">药品条形码图片文件</param>
    /// <returns>匹配验证结果</returns>
    [HttpPost("validate-drug")]
    public async Task<ActionResult<TaskDrugMatchingResult>> ValidateTaskDrugMatch(
        [Required] IFormFile executionTaskBarcode,
        [Required] IFormFile drugBarcode)
    {
        try
        {
            // 输入验证
            if (executionTaskBarcode == null || executionTaskBarcode.Length == 0)
            {
                return BadRequest("执行任务条形码图片不能为空");
            }

            if (drugBarcode == null || drugBarcode.Length == 0)
            {
                return BadRequest("药品条形码图片不能为空");
            }

            // 验证文件大小（最大10MB）
            const long maxFileSize = 10 * 1024 * 1024;
            if (executionTaskBarcode.Length > maxFileSize || drugBarcode.Length > maxFileSize)
            {
                return BadRequest("图片文件过大，最大允许10MB");
            }

            _logger.LogInformation("开始处理药品匹配验证请求");

            // 调用服务进行验证
            using var taskBarcodeStream = executionTaskBarcode.OpenReadStream();
            using var drugBarcodeStream = drugBarcode.OpenReadStream();

            var result = await _barcodeMatchingService.ValidateTaskDrugMatchAsync(
                taskBarcodeStream, 
                drugBarcodeStream);

            _logger.LogInformation("药品匹配验证完成: 结果={IsMatched}", result.IsMatched);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药品匹配验证API调用失败");
            return StatusCode(500, new TaskDrugMatchingResult
            {
                IsMatched = false,
                ErrorMessage = "服务器内部错误，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 查找最近的待执行护理任务
    /// </summary>
    /// <param name="patientBarcode">患者条形码图片文件</param>
    /// <returns>最近的执行任务</returns>
    [HttpPost("find-nearest-task")]
    public async Task<ActionResult<CareFlow.Core.Models.Nursing.ExecutionTask>> FindNearestDataCollectionTask(
        [Required] IFormFile patientBarcode)
    {
        try
        {
            // 输入验证
            if (patientBarcode == null || patientBarcode.Length == 0)
            {
                return BadRequest("患者条形码图片不能为空");
            }

            // 验证文件大小（最大10MB）
            const long maxFileSize = 10 * 1024 * 1024;
            if (patientBarcode.Length > maxFileSize)
            {
                return BadRequest("图片文件过大，最大允许10MB");
            }

            _logger.LogInformation("开始查找最近的护理任务");

            // 调用服务
            using var patientBarcodeStream = patientBarcode.OpenReadStream();
            var task = await _barcodeMatchingService.FindNearestDataCollectionTaskAsync(patientBarcodeStream);

            if (task == null)
            {
                return NotFound("未找到符合条件的最近任务");
            }

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查找最近护理任务API调用失败");
            return StatusCode(500, "服务器内部错误");
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
            Endpoints = new object[]
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
                        toleranceMinutes = "允许的时间偏差分钟数（可选，默认30分钟，范围0-1440分钟）",
                        checkTime = "是否检查时间（可选，默认true）"
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
                },
                new
                {
                    Method = "POST",
                    Path = "/api/barcodematching/validate-drug",
                    Description = "验证执行任务和药品条形码匹配",
                    Parameters = new
                    {
                        executionTaskBarcode = "执行任务条形码图片文件（支持png、jpg、jpeg、bmp、gif格式，最大10MB）",
                        drugBarcode = "药品条形码图片文件（支持png、jpg、jpeg、bmp、gif格式，最大10MB）"
                    },
                    Response = new
                    {
                        IsMatched = "bool - 是否匹配成功",
                        TaskId = "long - 任务ID",
                        ScannedDrugId = "string - 扫描的药品ID",
                        ScannedDrugName = "string - 扫描的药品名称",
                        TotalDrugs = "int - 总药品数",
                        ConfirmedDrugs = "int - 已确认药品数",
                        IsFullyCompleted = "bool - 是否全部完成",
                        ErrorMessage = "string - 错误消息"
                    }
                },
                new
                {
                    Method = "POST",
                    Path = "/api/barcodematching/find-nearest-task",
                    Description = "查找最近的待执行护理任务",
                    Parameters = new
                    {
                        patientBarcode = "患者条形码图片文件（支持png、jpg、jpeg、bmp、gif格式，最大10MB）"
                    },
                    Response = new
                    {
                        Id = "long - 任务ID",
                        PatientId = "string - 患者ID",
                        Category = "string - 任务类别",
                        PlannedStartTime = "DateTime - 计划开始时间",
                        Status = "string - 状态"
                    }
                }
            },
            ExampleUsage = new
            {
                CurlCommand = "curl -X POST \"/api/barcodematching/validate\" -H \"Content-Type: multipart/form-data\" -F \"executionTaskBarcode=@task_barcode.png\" -F \"patientBarcode=@patient_barcode.png\" -F \"toleranceMinutes=30\" -F \"checkTime=true\""
            }
        });
    }
}