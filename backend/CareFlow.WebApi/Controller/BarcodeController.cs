using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Barcode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarcodeController : ControllerBase
{
    private readonly IBarcodeService _barcodeService;
    private readonly IRecordValidationService _recordValidationService;
    private readonly IRepository<BarcodeIndex, string> _barcodeRepository;
    private readonly ILogger<BarcodeController> _logger;

    public BarcodeController(
        IBarcodeService barcodeService, 
        IRecordValidationService recordValidationService,
        IRepository<BarcodeIndex, string> barcodeRepository,
        ILogger<BarcodeController> logger)
    {
        _barcodeService = barcodeService;
        _recordValidationService = recordValidationService;
        _barcodeRepository = barcodeRepository;
        _logger = logger;
    }

    // 1. 生成接口：传入 { "tableName": "MedicalOrder", "recordId": "1001" }
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] BarcodeIndex request)
    {
        try 
        {
            var result = await _barcodeService.GenerateAndSaveBarcodeAsync(request, true);
            
            // 更新或创建数据库记录
            var indexId = $"{request.TableName}-{request.RecordId}";
            var existingIndex = await _barcodeRepository.GetByIdAsync(indexId);
            
            if (existingIndex != null)
            {
                existingIndex.ImagePath = result.FilePath;
                existingIndex.ImageSize = result.FileSize;
                existingIndex.ImageMimeType = result.MimeType;
                existingIndex.ImageGeneratedAt = result.GeneratedAt;
                await _barcodeRepository.UpdateAsync(existingIndex);
            }
            else
            {
                var newIndex = new BarcodeIndex
                {
                    Id = indexId,
                    TableName = request.TableName,
                    RecordId = request.RecordId,
                    ImagePath = result.FilePath,
                    ImageSize = result.FileSize,
                    ImageMimeType = result.MimeType,
                    ImageGeneratedAt = result.GeneratedAt
                };
                await _barcodeRepository.AddAsync(newIndex);
            }
            
            return File(result.ImageData, result.MimeType, $"{request.TableName}-{request.RecordId}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成条形码失败: {TableName}-{RecordId}", request.TableName, request.RecordId);
            return BadRequest(new { message = ex.Message });
        }
    }

    // 1.1 获取已保存的条形码图片
    [HttpGet("image/{tableName}/{recordId}")]
    public async Task<IActionResult> GetBarcodeImage(string tableName, string recordId)
    {
        try
        {
            var indexId = $"{tableName}-{recordId}";
            var barcodeIndex = await _barcodeRepository.GetByIdAsync(indexId);

            if (barcodeIndex == null || string.IsNullOrEmpty(barcodeIndex.ImagePath))
            {
                // 如果没有保存的条形码记录，生成一个新的
                return await Generate(new BarcodeIndex { TableName = tableName, RecordId = recordId });
            }

            var imageData = await _barcodeService.GetBarcodeImageAsync(barcodeIndex.ImagePath);
            if (imageData == null)
            {
                // 如果文件不存在，重新生成
                return await Generate(new BarcodeIndex { TableName = tableName, RecordId = recordId });
            }

            return File(imageData, barcodeIndex.ImageMimeType ?? "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取条形码图片失败: {TableName}-{RecordId}", tableName, recordId);
            return BadRequest(new { message = ex.Message });
        }
    }

    // 1.2 获取条形码信息（不返回图片，只返回元数据）
    [HttpGet("info/{tableName}/{recordId}")]
    public async Task<IActionResult> GetBarcodeInfo(string tableName, string recordId)
    {
        try
        {
            var indexId = $"{tableName}-{recordId}";
            var barcodeIndex = await _barcodeRepository.GetByIdAsync(indexId);

            if (barcodeIndex == null)
            {
                return NotFound(new { message = "条形码索引不存在" });
            }

            return Ok(new
            {
                tableName = barcodeIndex.TableName,
                recordId = barcodeIndex.RecordId,
                imagePath = barcodeIndex.ImagePath,
                imageSize = barcodeIndex.ImageSize,
                mimeType = barcodeIndex.ImageMimeType,
                generatedAt = barcodeIndex.ImageGeneratedAt,
                createdAt = barcodeIndex.CreateTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取条形码信息失败: {TableName}-{RecordId}", tableName, recordId);
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