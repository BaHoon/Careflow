namespace CareFlow.Core.Models;

public class BarcodeIndex : EntityBase<string>
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    
    /// <summary>
    /// 条形码图片的文件路径（相对路径）
    /// 例如：barcodes/ExecutionTasks/2024/12/ET-123.png
    /// </summary>
    public string? ImagePath { get; set; }
    
    /// <summary>
    /// 条形码图片的文件大小（字节）
    /// </summary>
    public long? ImageSize { get; set; }
    
    /// <summary>
    /// 条形码图片的MIME类型
    /// 例如：image/png, image/jpeg
    /// </summary>
    public string? ImageMimeType { get; set; }
    
    /// <summary>
    /// 图片生成时间
    /// </summary>
    public DateTime? ImageGeneratedAt { get; set; }

    /// <summary>
    /// 表名对照字典：将大写表名映射到正确的表名
    /// 由于条形码编码只支持大写字母，需要在解析时转换回正确的表名
    /// </summary>
    private static readonly Dictionary<string, string> TableNameMapping = new()
    {
        // Organization (人员与组织)
        { "DEPARTMENTS", "Departments" },
        { "STAFFS", "Staffs" },
        { "DOCTORS", "Doctors" },
        { "NURSES", "Nurses" },
        
        // Space (空间管理)
        { "WARDS", "Wards" },
        { "BEDS", "Beds" },
        
        // Patient (患者信息)
        { "PATIENTS", "Patients" },
        
        // Medical (医嘱体系)
        { "MEDICALORDERS", "MedicalOrders" },
        { "MEDICATIONORDERS", "MedicationOrders" },
        { "INSPECTIONORDERS", "InspectionOrders" },
        { "SURGICALORDERS", "SurgicalOrders" },
        { "OPERATIONORDERS", "OperationOrders" },
        { "INSPECTIONREPORTS", "InspectionReports" },
        { "HOSPITALTIMESLOTS", "HospitalTimeSlots" },
        { "DRUGS", "Drugs" },
        
        // Nursing (护理模块)
        { "VITALSIGNSRECORDS", "VitalSignsRecords" },
        { "NURSINGCARENOTES", "NursingCareNotes" },
        { "EXECUTIONTASKS", "ExecutionTasks" },
        { "SHIFTTYPES", "ShiftTypes" },
        { "NURSEROSTERS", "NurseRosters" }
    };

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

        string upperTableName = codeText.Substring(0, firstDashIndex);
        string recordId = codeText.Substring(firstDashIndex + 1);

        if (string.IsNullOrWhiteSpace(upperTableName) || string.IsNullOrWhiteSpace(recordId))
            throw new ArgumentException($"表名或记录ID不能为空，解析结果: TableName='{upperTableName}', RecordId='{recordId}'");

        // 将大写表名转换为正确的表名
        string correctTableName = ConvertTableName(upperTableName.Trim());

        return new BarcodeIndex 
        { 
            TableName = correctTableName, 
            RecordId = recordId.Trim() 
        };
    }

    /// <summary>
    /// 将大写表名转换为正确的表名
    /// </summary>
    /// <param name="upperTableName">大写的表名</param>
    /// <returns>正确的表名</returns>
    private static string ConvertTableName(string upperTableName)
    {
        if (TableNameMapping.TryGetValue(upperTableName, out string? correctTableName))
        {
            return correctTableName;
        }
        
        // 如果找不到映射，保持原样并记录警告
        Console.WriteLine($"[条形码解析警告] 未找到表名映射: '{upperTableName}'，保持原样返回");
        return upperTableName;
    }
}