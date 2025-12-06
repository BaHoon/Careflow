using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarcodeController : ControllerBase
{
    private readonly IBarcodeService _barcodeService;

    public BarcodeController(IBarcodeService barcodeService)
    {
        _barcodeService = barcodeService;
    }

    // 1. 生成接口：传入 { "tableName": "MedicalOrder", "recordId": "1001" }
    [HttpPost("generate")]
    public IActionResult Generate([FromBody] BarcodeIndex request)
    {
        try 
        {
            var imageBytes = _barcodeService.GenerateBarcode(request);
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "识别失败: " + ex.Message });
        }
    }
}