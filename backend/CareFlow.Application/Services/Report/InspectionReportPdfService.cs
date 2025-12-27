using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CareFlow.Core.Models.Medical;

namespace CareFlow.Application.Services.Report;

/// <summary>
/// 检查报告 PDF 生成服务
/// </summary>
public class InspectionReportPdfService
{
    /// <summary>
    /// 生成检查报告 PDF
    /// </summary>
    /// <param name="report">检查报告数据</param>
    /// <param name="outputPath">输出文件路径</param>
    public void GenerateReportPdf(InspectionReportData reportData, string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("SimSun"));

                // 页眉 - 简洁标准样式
                page.Header()
                    .PaddingBottom(15)
                    .BorderBottom(2)
                    .BorderColor(Colors.Black)
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Text("医学影像检查报告")
                            .FontSize(18)
                            .Bold();
                        
                        column.Item().AlignCenter().PaddingTop(8).Text(reportData.HospitalName)
                            .FontSize(11);
                    });

                // 主体内容
                page.Content()
                    .PaddingTop(20)
                    .Column(column =>
                    {
                        // 患者基本信息 - 表格样式
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);  // 标签列
                                columns.RelativeColumn(1);    // 内容列
                                columns.ConstantColumn(80);  // 标签列
                                columns.RelativeColumn(1);    // 内容列
                            });

                            // 第一行
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("患者姓名").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.PatientName);
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("性别").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.PatientGender);

                            // 第二行
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("年龄").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text($"{reportData.PatientAge}岁");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("病历号").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.PatientId);

                            // 第三行
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("科室").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.Department);
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("床号").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.BedNumber);

                            // 第四行
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("送检医生").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.RequestingDoctor);
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("检查号").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.RisLisId);
                        });

                        // 检查信息
                        column.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(1);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(1);
                            });

                            // 检查项目和日期
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("检查项目").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text($"{reportData.ItemName} ({reportData.ItemCode})");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("检查日期").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.ExaminationDate.ToString("yyyy-MM-dd HH:mm"));

                            // 报告日期
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("报告日期").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text(reportData.ReportDate.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("").SemiBold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                                .Text("");
                        });

                        // 检查所见
                        column.Item().PaddingTop(20).Column(col =>
                        {
                            col.Item().PaddingBottom(5).Text("检查所见：").SemiBold().FontSize(11);
                            col.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Medium)
                                .Padding(12)
                                .MinHeight(120)
                                .AlignTop()
                                .Text(reportData.Findings ?? "（无）")
                                .FontSize(10)
                                .LineHeight(1.8f);
                        });

                        // 诊断意见
                        column.Item().PaddingTop(15).Column(col =>
                        {
                            col.Item().PaddingBottom(5).Text("诊断意见：").SemiBold().FontSize(11);
                            col.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Medium)
                                .Padding(12)
                                .MinHeight(80)
                                .AlignTop()
                                .Text(reportData.Impression ?? "（无）")
                                .FontSize(10)
                                .LineHeight(1.8f);
                        });

                        // 报告医生签名区
                        column.Item().PaddingTop(30).PaddingRight(30).Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(200).Column(col =>
                            {
                                col.Item().PaddingBottom(8).Text($"报告医生：{reportData.ReviewerName ?? "检查站"}");
                                col.Item().Text($"审核日期：{reportData.ReportDate:yyyy-MM-dd}");
                            });
                        });
                    });

                // 页脚
                page.Footer()
                    .PaddingTop(10)
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Medium)
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(TextStyle.Default.FontSize(8));
                        text.Span("此报告仅供临床参考   ");
                        text.Span("报告编号：").SemiBold();
                        text.Span(reportData.ReportNumber);
                        text.Span($"   打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    });
            });
        });
        
        document.GeneratePdf(outputPath);
    }
}

/// <summary>
/// 检查报告数据模型（用于 PDF 生成）
/// </summary>
public class InspectionReportData
{
    public string HospitalName { get; set; } = "医疗机构";
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientGender { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public string Department { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public string RequestingDoctor { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string RisLisId { get; set; } = string.Empty;
    public DateTime ExaminationDate { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? ReviewerName { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
}
