using CareFlow.Core.Enums;

namespace CareFlow.Application.DTOs.Inspection;

/// <summary>
/// 创建检查报告 DTO (模拟从 RIS/LIS 接收报告)
/// </summary>
public class CreateInspectionReportDto
{
    public long OrderId { get; set; }
    public string RisLisId { get; set; } = null!;
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? ReviewerId { get; set; }
    public InspectionSource ReportSource { get; set; }
}

/// <summary>
/// 检查报告详情响应 DTO
/// </summary>
public class InspectionReportDetailDto
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string PatientId { get; set; } = null!;
    public string PatientName { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public string RisLisId { get; set; } = null!;
    public DateTime ReportTime { get; set; }
    public InspectionReportStatus ReportStatus { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public InspectionSource ReportSource { get; set; }
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 更新报告状态 DTO
/// </summary>
public class UpdateReportStatusDto
{
    public long ReportId { get; set; }
    public InspectionReportStatus Status { get; set; }
}
