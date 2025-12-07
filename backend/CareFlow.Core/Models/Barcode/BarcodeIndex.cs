namespace CareFlow.Core.Models;

public class BarcodeIndex : EntityBase<string>
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;

    // 修改点 1: 分隔符改为 '-' (因为 Code39Standard 不支持冒号)
    public override string ToString() => $"{TableName}-{RecordId}";

    public static BarcodeIndex Parse(string codeText)
    {
        // 修改点 2: 按 '-' 分割
        // 注意：如果你的 TableName 或 RecordId 本身包含 '-'，这里需要更复杂的处理
        // 简单起见，假设 TableName 不含 '-'
        var parts = codeText.Split('-'); 
        
        if (parts.Length < 2) // 防止数组越界
             throw new ArgumentException($"无效的数据格式: {codeText}");

        return new BarcodeIndex { TableName = parts[0], RecordId = parts[1] };
    }
}