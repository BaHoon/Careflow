namespace CareFlow.Core.Interfaces;

public interface IRecordValidationService
{
    /// <summary>
    /// 验证指定表中是否存在该主键ID的记录
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="recordId">记录ID</param>
    /// <returns>如果记录存在则返回true，否则返回false</returns>
    Task<bool> RecordExistsAsync(string tableName, string recordId);
    
    /// <summary>
    /// 获取支持的表名列表
    /// </summary>
    /// <returns>支持的表名集合</returns>
    IEnumerable<string> GetSupportedTables();
}