using Aspose.BarCode.Generation;
using Aspose.BarCode.BarCodeRecognition;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Barcode;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CareFlow.Infrastructure.Services;

public class AsposeBarcodeService : IBarcodeService
{
    private readonly IRecordValidationService _recordValidationService;
    private readonly CareFlow.Core.Models.Barcode.BarcodeSettings _barcodeSettings;
    private readonly ILogger<AsposeBarcodeService> _logger;

    public AsposeBarcodeService(
        IRecordValidationService recordValidationService, 
        IOptions<CareFlow.Core.Models.Barcode.BarcodeSettings> barcodeSettings,
        ILogger<AsposeBarcodeService> logger)
    {
        _recordValidationService = recordValidationService;
        _barcodeSettings = barcodeSettings.Value;
        _logger = logger;
    }

    public async Task<byte[]> GenerateBarcodeAsync(BarcodeIndex indexData)
    {
        // 验证记录是否存在
        bool recordExists = await _recordValidationService.RecordExistsAsync(indexData.TableName, indexData.RecordId);
        
        if (!recordExists)
        {
            throw new InvalidOperationException($"记录不存在：表 '{indexData.TableName}' 中没有找到ID为 '{indexData.RecordId}' 的记录，无法生成条形码。");
        }

        // 这里的 ToString() 现在会使用 '-' 分隔符
        string codeText = indexData.ToString();
        
        // 调试信息：记录实际编码的文本
        Console.WriteLine($"[条形码生成] 编码文本: '{codeText}' (长度: {codeText.Length})");
        Console.WriteLine($"[条形码生成] 表名: '{indexData.TableName}', 记录ID: '{indexData.RecordId}'");

        // --- 修改点 1: 改为 Code39Standard ---
        // 这是试用版唯一不加水印/乱码的格式
        using var generator = new BarcodeGenerator(EncodeTypes.Code39Standard, codeText);
        // 1. 设置条纹粗细 (XDimension)
        generator.Parameters.Barcode.XDimension.Pixels = 2; 
        
        // 2. 设置条纹高度
        generator.Parameters.Barcode.BarHeight.Pixels = 60; // 稍微高一点有助于识别

        // 3. 【关键修改】不要设置 ImageWidth.Pixels，改用 AutoSizeMode
        generator.Parameters.AutoSizeMode = AutoSizeMode.Nearest;

        // 4. 设置左右边距 (Quiet Zones)，防止贴边导致识别失败
        generator.Parameters.Barcode.Padding.Left.Pixels = 10;
        generator.Parameters.Barcode.Padding.Right.Pixels = 10;

        using var ms = new MemoryStream();
        generator.Save(ms, BarCodeImageFormat.Png);
        var result = ms.ToArray();
        
        Console.WriteLine($"[条形码生成] 生成成功，图片大小: {result.Length} bytes");
        return result;
    }

    public async Task<BarcodeGenerationResult> GenerateAndSaveBarcodeAsync(BarcodeIndex indexData, bool saveToFile = true)
    {
        // 生成条形码图片字节流
        var imageData = await GenerateBarcodeAsync(indexData);
        var result = new BarcodeGenerationResult
        {
            ImageData = imageData,
            FileSize = imageData.Length,
            MimeType = "image/png",
            GeneratedAt = DateTime.UtcNow
        };

        if (saveToFile)
        {
            try
            {
                // 生成文件路径: barcodes/TableName/YYYY/MM/ID.png
                var fileName = GenerateFileName(indexData);
                var relativePath = GenerateRelativePath(indexData.TableName, fileName);
                var fullPath = Path.Combine(_barcodeSettings.StoragePath, relativePath);

                // 确保目录存在
                var directory = Path.GetDirectoryName(fullPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 保存文件
                await File.WriteAllBytesAsync(fullPath, imageData);
                result.FilePath = relativePath;

                _logger.LogInformation("条形码图片已保存: {FilePath}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存条形码图片失败: {TableName}-{RecordId}", 
                    indexData.TableName, indexData.RecordId);
                // 即使保存失败，也返回图片数据
            }
        }

        return result;
    }

    public async Task<byte[]?> GetBarcodeImageAsync(string imagePath)
    {
        try
        {
            var fullPath = Path.Combine(_barcodeSettings.StoragePath, imagePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("条形码图片文件不存在: {FilePath}", fullPath);
                return null;
            }

            return await File.ReadAllBytesAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取条形码图片失败: {ImagePath}", imagePath);
            return null;
        }
    }

    public async Task DeleteBarcodeImageAsync(string imagePath)
    {
        try
        {
            var fullPath = Path.Combine(_barcodeSettings.StoragePath, imagePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("已删除条形码图片: {FilePath}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除条形码图片失败: {ImagePath}", imagePath);
            throw;
        }
    }

    public BarcodeIndex RecognizeBarcode(Stream imageStream)
    {
        if (imageStream.CanSeek && imageStream.Position != 0)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
        }

        // 识别 Code39Standard 格式的条形码
        using var reader = new BarCodeReader(imageStream, DecodeType.Code39Standard);

        BarCodeResult[] results = reader.ReadBarCodes();

        if (results.Length == 0)
        {
            throw new Exception("无法识别条形码，请确保图片清晰且为Code39格式");
        }

        string codeText = results[0].CodeText;
        
        try
        {
            // Parse方法会自动将大写表名转换为正确的表名
            // 例如：EXECUTIONTASKS -> ExecutionTasks
            var barcodeIndex = BarcodeIndex.Parse(codeText);
            
            // 记录解析结果用于调试（现在显示转换后的正确表名）
            Console.WriteLine($"[条形码解析] 原始文本: '{codeText}' -> 正确表名: '{barcodeIndex.TableName}', 记录ID: '{barcodeIndex.RecordId}'");
            
            return barcodeIndex;
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"条形码格式解析失败 - 识别到的文本: '{codeText}', 错误: {ex.Message}");
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 生成文件名
    /// </summary>
    private string GenerateFileName(BarcodeIndex indexData)
    {
        // 使用表名和ID生成文件名，确保文件名安全
        var safeTableName = SanitizeFileName(indexData.TableName);
        var safeRecordId = SanitizeFileName(indexData.RecordId);
        return $"{safeTableName}-{safeRecordId}.png";
    }

    /// <summary>
    /// 生成相对路径
    /// </summary>
    private string GenerateRelativePath(string tableName, string fileName)
    {
        var now = DateTime.UtcNow;
        var yearMonth = $"{now.Year:D4}/{now.Month:D2}";
        return Path.Combine(tableName, yearMonth, fileName).Replace('\\', '/');
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    private string SanitizeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(input.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized; // 限制长度
    }

    #endregion
}