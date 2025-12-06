using Aspose.BarCode.Generation;
using Aspose.BarCode.BarCodeRecognition;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using System.IO;

namespace CareFlow.Infrastructure.Services;

public class AsposeBarcodeService : IBarcodeService
{
    // 生成条形码（这部分通常保持不变，但为了完整性我一并列出）
    public byte[] GenerateBarcode(BarcodeIndex indexData)
    {
        string codeText = indexData.ToString();

        // 使用 Code128 编码
        using var generator = new BarcodeGenerator(EncodeTypes.Code128, codeText);
        
        // 设置参数
        generator.Parameters.Barcode.XDimension.Pixels = 2;
        generator.Parameters.Barcode.BarHeight.Pixels = 40;

        using var ms = new MemoryStream();
        // 保存为 PNG 格式
        generator.Save(ms, BarCodeImageFormat.Png);
        return ms.ToArray();
    }

    // 识别条形码（这里是修复重点）
    public BarcodeIndex RecognizeBarcode(Stream imageStream)
    {
        // 确保流的位置在起始点（这是处理上传文件的常见保险措施）
        if (imageStream.CanSeek && imageStream.Position != 0)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
        }

        // 初始化读取器，指定只尝试识别 Code128 以提高性能
        using var reader = new BarCodeReader(imageStream, DecodeType.Code128);

        // --- 修复点 1: API 变更 ---
        // 新版 ReadBarCodes() 返回 BarCodeResult[] 数组，不再返回 bool
        BarCodeResult[] results = reader.ReadBarCodes();

        // --- 修复点 2: 检查结果 ---
        if (results.Length > 0)
        {
            // --- 修复点 3: 获取文本 ---
            // 使用 results[0].CodeText 属性，而不是 GetCodeText() 方法
            string codeText = results[0].CodeText;
            
            // 解析回对象
            return BarcodeIndex.Parse(codeText);
        }

        throw new Exception("无法从图片中识别出有效的条形码");
    }
}