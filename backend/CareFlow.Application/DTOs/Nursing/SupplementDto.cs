namespace CareFlow.Application.DTOs.Nursing;

/// <summary>
/// 添加护理记录补充说明请求
/// </summary>
public class AddSupplementDto
{
    /// <summary>
    /// 护理任务ID
    /// </summary>
    public long NursingTaskId { get; set; }
    
    /// <summary>
    /// 补充护士ID
    /// </summary>
    public string SupplementNurseId { get; set; } = string.Empty;
    
    /// <summary>
    /// 补充内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 补充类型：Correction(更正), Addition(补充)
    /// </summary>
    public string SupplementType { get; set; } = "Addition";
}

/// <summary>
/// 护理记录补充说明响应
/// </summary>
public class SupplementDto
{
    public long Id { get; set; }
    public long NursingTaskId { get; set; }
    public string SupplementNurseId { get; set; } = string.Empty;
    public string SupplementNurseName { get; set; } = string.Empty;
    public DateTime SupplementTime { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SupplementType { get; set; } = string.Empty;
}
