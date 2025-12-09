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

        // --- 修改点 1: 改为 Code39Standard ---
        // 这是试用版唯一不加水印/乱码的格式
        using var generator = new BarcodeGenerator(EncodeTypes.Code39Standard, codeText);
        
        // Code39 比较宽，建议把像素密度稍微调小一点，或者让图片宽一点
        generator.Parameters.Barcode.XDimension.Pixels = 2;
        generator.Parameters.Barcode.BarHeight.Pixels = 40;

        using var ms = new MemoryStream();
        generator.Save(ms, BarCodeImageFormat.Png);
        return ms.ToArray();
    }

    public BarcodeIndex RecognizeBarcode(Stream imageStream)
    {
        if (imageStream.CanSeek && imageStream.Position != 0)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
        }

        // --- 修改点 2: 改为识别 Code39Standard ---
        using var reader = new BarCodeReader(imageStream, DecodeType.Code39Standard);

        BarCodeResult[] results = reader.ReadBarCodes();

        if (results.Length > 0)
        {
            string codeText = results[0].CodeText;
            return BarcodeIndex.Parse(codeText);
        }

        throw new Exception("无法识别条形码，请确保图片清晰且为Code39格式");
    }
}