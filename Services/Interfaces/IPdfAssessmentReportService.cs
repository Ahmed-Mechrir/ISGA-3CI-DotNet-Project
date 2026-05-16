using AsvsSecurityAuditor.ViewModels.Reports;

namespace AsvsSecurityAuditor.Services.Interfaces;

public interface IPdfAssessmentReportService
{
    byte[] GeneratePdf(BenchmarkReportViewModel model);
}
