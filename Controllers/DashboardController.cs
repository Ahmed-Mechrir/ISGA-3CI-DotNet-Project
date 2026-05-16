using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Repositories.Interfaces;
using AsvsSecurityAuditor.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Controllers;

[Authorize(Policy = "Auditor")]
public class DashboardController : Controller
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IReportService _reportService;

    public DashboardController(IAssessmentRepository assessmentRepository, IReportService reportService)
    {
        _assessmentRepository = assessmentRepository;
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(int? assessmentId, CancellationToken ct)
    {
        var userId = UserId();
        var list = await _assessmentRepository.ListForUserAsync(userId, ct);

        ViewBag.Assessments = list.ToList();

        if (!assessmentId.HasValue && list.Count > 0)
            assessmentId = list[0].Id;

        ViewBag.SelectedAssessmentId = assessmentId;
        DashboardStatsDto? model = null;

        if (!assessmentId.HasValue || !list.Any(a => a.Id == assessmentId.Value))
            ViewBag.ChartError = list.Count == 0
                ? "Create an assessment to see the dashboard."
                : "Select a valid assessment.";
        else
        {
            ViewBag.Title = list.First(a => a.Id == assessmentId.Value).Title;
            try
            {
                model = await _reportService.BuildDashboardStatsAsync(assessmentId.Value, userId, ct);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.ChartError = ex.Message;
            }
        }

        return View(model);
    }

    private string UserId() =>
        HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
