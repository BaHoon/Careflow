namespace CareFlow.Application.Interfaces;

/// <summary>
/// 条形码匹配服务接口
/// </summary>
public interface IBarcodeMatchingService
{
    /// <summary>
    /// 验证执行任务和患者的条形码匹配
    /// </summary>
    /// <param name="executionTaskBarcodeImage">执行任务条形码图片流</param>
    /// <param name="patientBarcodeImage">患者条形码图片流</param>
    /// <param name="toleranceMinutes">执行时间允许的偏差分钟数（默认30分钟）</param>
    /// <returns>匹配结果</returns>
    Task<BarcodeMatchingResult> ValidateBarcodeMatchAsync(
        Stream executionTaskBarcodeImage, 
        Stream patientBarcodeImage, 
        int toleranceMinutes = 30);
}

/// <summary>
/// 条形码匹配结果
/// </summary>
public class BarcodeMatchingResult
{
    /// <summary>
    /// 是否匹配成功
    /// </summary>
    public bool IsMatched { get; set; }
    
    /// <summary>
    /// 执行任务ID
    /// </summary>
    public long ExecutionTaskId { get; set; }
    
    /// <summary>
    /// 患者ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// 计划执行时间
    /// </summary>
    public DateTime PlannedStartTime { get; set; }
    
    /// <summary>
    /// 当前扫码时间
    /// </summary>
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 时间差异（分钟）
    /// </summary>
    public double TimeDifferenceMinutes { get; set; }
    
    /// <summary>
    /// 详细的验证信息
    /// </summary>
    public BarcodeValidationDetails ValidationDetails { get; set; } = new();
    
    /// <summary>
    /// 错误消息（如果匹配失败）
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// 条形码验证详情
/// </summary>
public class BarcodeValidationDetails
{
    /// <summary>
    /// 执行任务条形码解码是否成功
    /// </summary>
    public bool ExecutionTaskBarcodeDecoded { get; set; }
    
    /// <summary>
    /// 患者条形码解码是否成功
    /// </summary>
    public bool PatientBarcodeDecoded { get; set; }
    
    /// <summary>
    /// 解码出的执行任务ID
    /// </summary>
    public string DecodedExecutionTaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// 解码出的患者ID
    /// </summary>
    public string DecodedPatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行任务是否存在
    /// </summary>
    public bool ExecutionTaskExists { get; set; }
    
    /// <summary>
    /// 患者是否存在
    /// </summary>
    public bool PatientExists { get; set; }
    
    /// <summary>
    /// 患者ID是否匹配
    /// </summary>
    public bool PatientIdMatched { get; set; }
    
    /// <summary>
    /// 执行时间是否在允许范围内
    /// </summary>
    public bool ExecutionTimeMatched { get; set; }
    
    /// <summary>
    /// 执行任务状态是否允许执行
    /// </summary>
    public bool ExecutionTaskStatusValid { get; set; }
}