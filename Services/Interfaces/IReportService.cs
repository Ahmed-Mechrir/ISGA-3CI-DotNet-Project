using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.ViewModels.Reports;

namespace AsvsSecurityAuditor.Services.Interfaces;

public interface IReportService
{
    Task<BenchmarkReportViewModel> BuildBenchmarkAsync(int assessmentId, string userId, CancellationToken ct = default);
    Task<DashboardStatsDto> BuildDashboardStatsAsync(int assessmentId, string userId, CancellationToken ct = default);
}
