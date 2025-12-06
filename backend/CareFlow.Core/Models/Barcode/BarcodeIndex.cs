namespace CareFlow.Core.Models;

/// <summary>
/// 索引引用模型：用于在条形码和数据库记录之间建立映射
/// </summary>
public class BarcodeIndex
{
    public string TableName { get; set; } = string.Empty; // 真实表名
    public string RecordId { get; set; } = string.Empty;  // 主码ID (使用String以兼容Int或Guid)

    // 辅助方法：生成编码字符串 (格式: "TableName:RecordId")
    public override string ToString() => $"{TableName}:{RecordId}";

    // 辅助方法：从字符串解析
    public static BarcodeIndex Parse(string codeText)
    {
        var parts = codeText.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("无效的条形码数据格式");
            
        return new BarcodeIndex { TableName = parts[0], RecordId = parts[1] };
    }
}