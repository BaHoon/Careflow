using Aspose.BarCode.Generation;
using Aspose.BarCode.BarCodeRecognition;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using System.IO;

namespace CareFlow.Infrastructure.Services;

public class AsposeBarcodeService : IBarcodeService
{
    private readonly IRecordValidationService _recordValidationService;

    public AsposeBarcodeService(IRecordValidationService recordValidationService)
    {
        _recordValidationService = recordValidationService;
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
}