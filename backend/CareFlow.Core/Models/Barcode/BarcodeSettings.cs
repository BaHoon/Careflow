namespace CareFlow.Core.Models.Barcode;

/// <summary>
/// 条形码服务配置选项
/// </summary>
public class BarcodeSettings
{
    public const string SectionName = "Barcode";
    
    /// <summary>
    /// 条形码图片存储路径（相对于应用根目录）
    /// 例如：wwwroot/barcodes
    /// </summary>
    public string StoragePath { get; set; } = "wwwroot/barcodes";
    
    /// <summary>
    /// Web访问的基础URL路径
    /// 例如：/barcodes
    /// </summary>
    public string BaseUrl { get; set; } = "/barcodes";
    
    /// <summary>
    /// 最大文件大小（字节），默认1MB
    /// </summary>
    public long MaxFileSize { get; set; } = 1048576;
    
    /// <summary>
    /// 是否启用自动清理过期文件
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;
    
    /// <summary>
    /// 文件保留天数（用于自动清理）
    /// </summary>
    public int RetentionDays { get; set; } = 90;
}