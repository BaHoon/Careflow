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
    /// 识别条形码：输入图片流，输出表名和ID
    /// </summary>
    BarcodeIndex RecognizeBarcode(Stream imageStream);
}