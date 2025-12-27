using System.Text.Encodings.Web;
using System.Text.Json;

namespace CareFlow.Application.Common;

/// <summary>
/// 全局 JSON 序列化配置
/// </summary>
public static class JsonConfig
{
    /// <summary>
    /// 默认的 JSON 序列化选项（禁用 Unicode 转义，保留中文字符）
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
    {
        // 禁用 Unicode 转义，让中文、特殊字符正常显示
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        
        // 美化输出（可选，开发阶段建议开启）
        WriteIndented = false,
        
        // 属性命名策略（null 表示保持原命名）
        PropertyNamingPolicy = null,
        
        // 允许尾随逗号
        AllowTrailingCommas = true,
        
        // 忽略只读属性
        IgnoreReadOnlyProperties = false
    };
}
