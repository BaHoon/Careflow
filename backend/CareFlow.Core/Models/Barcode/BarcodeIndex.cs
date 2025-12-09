namespace CareFlow.Core.Models;

public class BarcodeIndex : EntityBase<string>
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;

    // 条形码2.0格式: 使用 '-' 分隔符，并将表名转为大写 (因为 Code39Standard 不支持小写字母)
    public override string ToString() => $"{TableName.ToUpper()}-{RecordId.ToUpper()}";

    public static BarcodeIndex Parse(string codeText)
    {
        if (string.IsNullOrWhiteSpace(codeText))
            throw new ArgumentException("条形码文本不能为空");

        // 按 '-' 分割，支持 TableName 或 RecordId 中包含多个 '-' 的情况
        // 只取第一个 '-' 作为分隔符，其余部分作为 RecordId
        int firstDashIndex = codeText.IndexOf('-');
        
        if (firstDashIndex <= 0 || firstDashIndex >= codeText.Length - 1)
            throw new ArgumentException($"无效的条形码格式，期望格式为 'TableName-RecordId'，实际格式: '{codeText}'");

        string tableName = codeText.Substring(0, firstDashIndex);
        string recordId = codeText.Substring(firstDashIndex + 1);

        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(recordId))
            throw new ArgumentException($"表名或记录ID不能为空，解析结果: TableName='{tableName}', RecordId='{recordId}'");

        return new BarcodeIndex 
        { 
            // 注意：解析时保持大小写原样，但在数据库查询时需要忽略大小写
            TableName = tableName.Trim(), 
            RecordId = recordId.Trim() 
        };
    }
}