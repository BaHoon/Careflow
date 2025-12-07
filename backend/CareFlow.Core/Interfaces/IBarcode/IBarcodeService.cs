using CareFlow.Core.Models;

namespace CareFlow.Core.Interfaces;

public interface IBarcodeService
{
    /// <summary>
    /// 生成条形码：输入表名和ID，输出图片字节流
    /// </summary>
    byte[] GenerateBarcode(BarcodeIndex indexData);

    /// <summary>
    /// 识别条形码：输入图片流，输出表名和ID
    /// </summary>
    BarcodeIndex RecognizeBarcode(Stream imageStream);
}