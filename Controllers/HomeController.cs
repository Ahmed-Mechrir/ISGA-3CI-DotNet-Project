using AsvsSecurityAuditor.Models;
using AsvsSecurityAuditor.Repositories.Interfaces;
using AsvsSecurityAuditor.ViewModels.Public;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Controllers;

public class HomeController : Controller
{
    private readonly IRequirementRepository _requirements;

    public HomeController(IRequirementRepository requirements) => _requirements = requirements;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _requirements.GetAllOrderedAsync(ct);
        var chapterNamesSorted = list
            .Select(r => r.Chapter)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vm = new PublicChecklistPageViewModel
        {
            DistinctChapterCount = chapterNamesSorted.Count,
            ChapterNamesSorted = chapterNamesSorted,
            RequirementsPerLevel = list
                .GroupBy(r => r.Level)
                .OrderBy(g => g.Key)
                .Select(g => new LevelCountSummary { Level = g.Key, Count = g.Count() })
                .ToList(),
            Rows = list.Select(r => new PublicChecklistRowViewModel
            {
                RequirementRef = r.RequirementRef,
                Chapter = r.Chapter,
                Area = r.Area,
                Level = r.Level,
                VerificationRequirement = r.VerificationRequirement,
                PreMarkedNotApplicable = r.PreMarkedNotApplicable
            }).ToList()
        };

        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
