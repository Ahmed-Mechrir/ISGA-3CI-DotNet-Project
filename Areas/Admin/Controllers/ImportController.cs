using AsvsSecurityAuditor.Services.Interfaces;
using AsvsSecurityAuditor.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ImportController : Controller
{
    private readonly ICsvImportService _importService;

    public ImportController(ICsvImportService importService) => _importService = importService;

    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose a CSV file.");
            return View("Index");
        }

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportFromStreamAsync(stream, ct);

        ViewBag.ImportResult = result;

        foreach (var err in result.Errors)
            ModelState.AddModelError(string.Empty, err);

        return View("Index");
    }
}
