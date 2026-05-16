using AsvsSecurityAuditor.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Controllers;

[Authorize(Policy = "Auditor")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly IPdfAssessmentReportService _pdfReportService;

    public ReportController(IReportService reportService, IPdfAssessmentReportService pdfReportService)
    {
        _reportService = reportService;
        _pdfReportService = pdfReportService;
    }

    public async Task<IActionResult> Benchmark(int assessmentId, CancellationToken ct)
    {
        try
        {
            var vm = await _reportService.BuildBenchmarkAsync(assessmentId, UserId(), ct);
            return View(vm);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    public async Task<IActionResult> Pdf(int assessmentId, CancellationToken ct)
    {
        try
        {
            var vm = await _reportService.BuildBenchmarkAsync(assessmentId, UserId(), ct);
            var pdf = _pdfReportService.GeneratePdf(vm);
            return File(pdf, "application/pdf", $"asvs-benchmark-{assessmentId}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    private string UserId() =>
        HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("User context missing.");
}
