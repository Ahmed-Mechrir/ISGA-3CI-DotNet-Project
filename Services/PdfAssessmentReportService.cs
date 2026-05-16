using AsvsSecurityAuditor.Services.Interfaces;
using AsvsSecurityAuditor.ViewModels.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AsvsSecurityAuditor.Services;

public class PdfAssessmentReportService : IPdfAssessmentReportService
{
    static PdfAssessmentReportService()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    public byte[] GeneratePdf(BenchmarkReportViewModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Column(c =>
                {
                    c.Item().Text("ASVS Security Auditor").FontSize(18).Bold();
                    c.Item().Text("Benchmark & compliance report").FontSize(11).FontColor(Colors.Grey.Medium);
                    c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Assessment: {model.AssessmentTitle}").SemiBold();
                    col.Item().Text($"Assessment ID: {model.AssessmentId}");
                    col.Item().Text($"Audience: {model.UserDisplay}");
                    col.Item().Text($"Generated (UTC): {model.GeneratedUtc:u}");
                    col.Spacing(12);

                    col.Item().Text("Summary metrics").FontSize(13).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        void Row(string label, string value)
                        {
                            t.Cell().PaddingVertical(4).Text(label).FontColor(Colors.Grey.Darken1);
                            t.Cell().PaddingVertical(4).Text(value).Bold();
                        }

                        Row("Applicable requirements", $"{model.ApplicableTotal}");
                        Row("Validated (Pass)", $"{model.Valid}");
                        Row("Failed (Not valid)", $"{model.NotValid}");
                        Row("Pending", $"{model.Pending}");
                        Row("Marked N/A (project-specific)", $"{model.UserMarkedNotApplicable}");
                        Row("Marked N/A in source checklist", $"{model.GlobalNotApplicable}");
                        Row("Compliance", $"{model.CompliancePct}%");
                        Row("Security score (0–100)", $"{model.SecurityScore}");
                        Row("Residual risk band", $"{model.RiskLevel}");
                    });

                    col.Spacing(16);
                    col.Item().Text("Chapter-by-chapter").FontSize(13).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().PaddingVertical(4).Text("Chapter").SemiBold().FontColor(Colors.Grey.Darken2);
                            h.Cell().PaddingVertical(4).Text("Applicable").SemiBold().FontColor(Colors.Grey.Darken2);
                            h.Cell().PaddingVertical(4).Text("Valid").SemiBold().FontColor(Colors.Grey.Darken2);
                            h.Cell().PaddingVertical(4).Text("Not valid").SemiBold().FontColor(Colors.Grey.Darken2);
                            h.Cell().PaddingVertical(4).Text("Pending").SemiBold().FontColor(Colors.Grey.Darken2);
                            h.Cell().PaddingVertical(4).Text("%").SemiBold().FontColor(Colors.Grey.Darken2);
                        });

                        foreach (var ch in model.Chapters)
                        {
                            table.Cell().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(ch.Chapter).FontSize(9);
                            table.Cell().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(ch.Applicable.ToString()).FontSize(9);
                            table.Cell().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(ch.Valid.ToString()).FontSize(9);
                            table.Cell().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(ch.NotValid.ToString()).FontSize(9);
                            table.Cell().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(ch.Pending.ToString()).FontSize(9);
                            table.Cell().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text($"{ch.CompliancePct}").FontSize(9).Bold();
                        }
                    });

                    col.Spacing(16);
                    col.Item().Text("Weak areas (prioritise remediation here)").FontSize(13).Bold();
                    if (model.WeakAreas.Count == 0)
                        col.Item().Text("Insufficient data.");
                    foreach (var w in model.WeakAreas)
                        col.Item().Text($"• {w.Chapter} — {w.CompliancePct}% — {w.Rationale}");
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium))
                    .Text("OWASP Application Security Verification Standard — ASVS Security Auditor export");
            });
        }).GeneratePdf();
    }
}
