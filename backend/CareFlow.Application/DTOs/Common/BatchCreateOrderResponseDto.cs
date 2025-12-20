namespace CareFlow.Application.DTOs.Common;

/// <summary>
/// 批量创建医嘱统一响应DTO
/// </summary>
public class BatchCreateOrderResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public BatchCreateOrderDataDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// 批量创建医嘱响应数据
/// </summary>
public class BatchCreateOrderDataDto
{
    public int CreatedCount { get; set; }
    public List<string> OrderIds { get; set; } = new();
    public int TaskCount { get; set; }
}
