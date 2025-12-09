using CareFlow.Core.Models;

namespace CareFlow.Core.Interfaces;

public interface IBarcodeService
{
    /// <summary>
    /// 生成条形码：输入表名和ID，输出图片字节流
    /// 会验证记录是否存在，如果记录不存在则抛出异常
    /// </summary>
    Task<byte[]> GenerateBarcodeAsync(BarcodeIndex indexData);

    /// <summary>
    /// 生成条形码并保存到文件系统
    /// </summary>
    /// <param name="indexData">条形码索引数据</param>
    /// <param name="saveToFile">是否保存到文件系统</param>
    /// <returns>条形码图片字节流和文件信息</returns>
    Task<BarcodeGenerationResult> GenerateAndSaveBarcodeAsync(BarcodeIndex indexData, bool saveToFile = true);

    /// <summary>
    /// 从文件系统获取条形码图片
    /// </summary>
    /// <param name="imagePath">图片文件路径</param>
    /// <returns>图片字节流，如果文件不存在返回null</returns>
    Task<byte[]?> GetBarcodeImageAsync(string imagePath);

    /// <summary>
    /// 识别条形码：输入图片流，输出表名和ID
    /// </summary>
    BarcodeIndex RecognizeBarcode(Stream imageStream);

    /// <summary>
    /// 删除条形码图片文件
    /// </summary>
    /// <param name="imagePath">图片文件路径</param>
    Task DeleteBarcodeImageAsync(string imagePath);
}

/// <summary>
/// 条形码生成结果
/// </summary>
public class BarcodeGenerationResult
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; } = "image/png";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}