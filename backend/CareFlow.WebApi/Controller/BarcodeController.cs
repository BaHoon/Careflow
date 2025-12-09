using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarcodeController : ControllerBase
{
    private readonly IBarcodeService _barcodeService;
    private readonly IRecordValidationService _recordValidationService;

    public BarcodeController(IBarcodeService barcodeService, IRecordValidationService recordValidationService)
    {
        _barcodeService = barcodeService;
        _recordValidationService = recordValidationService;
    }

    // 1. 生成接口：传入 { "tableName": "MedicalOrder", "recordId": "1001" }
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] BarcodeIndex request)
    {
        try 
        {
            var imageBytes = await _barcodeService.GenerateBarcodeAsync(request);
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 2. 识别接口：上传图片文件
    [HttpPost("recognize")]
    public IActionResult Recognize(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("请上传条形码图片");

        try
        {
            using var stream = file.OpenReadStream();
            var result = _barcodeService.RecognizeBarcode(stream);
            
            // 返回解析出的表名和ID，前端据此跳转到详情页
            return Ok(new 
            { 
                tableName = result.TableName, 
                recordId = result.RecordId,
                message = $"成功识别条形码: {result.TableName}-{result.RecordId}"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                message = "识别失败: " + ex.Message,
                error = "BARCODE_RECOGNITION_FAILED",
                details = $"上传的文件: {file.FileName}, 大小: {file.Length} bytes"
            });
        }
    }

    // 3. 获取支持的表名列表
    [HttpGet("supported-tables")]
    public IActionResult GetSupportedTables()
    {
        try
        {
            var tables = _recordValidationService.GetSupportedTables();
            return Ok(new { tables = tables.ToList() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 4. 验证记录是否存在（可选的调试接口）
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateRecord([FromBody] BarcodeIndex request)
    {
        try
        {
            bool exists = await _recordValidationService.RecordExistsAsync(request.TableName, request.RecordId);
            return Ok(new { tableName = request.TableName, recordId = request.RecordId, exists });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}