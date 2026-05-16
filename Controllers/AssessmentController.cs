using AsvsSecurityAuditor.Models.Enums;
using AsvsSecurityAuditor.Repositories.Interfaces;
using AsvsSecurityAuditor.Services.Interfaces;
using AsvsSecurityAuditor.ViewModels.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Controllers;

[Authorize(Policy = "Auditor")]
public class AssessmentController : Controller
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IAssessmentService _assessmentService;

    public AssessmentController(IAssessmentRepository assessmentRepository, IAssessmentService assessmentService)
    {
        _assessmentRepository = assessmentRepository;
        _assessmentService = assessmentService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = UserId();
        var list = await _assessmentRepository.ListForUserAsync(userId, ct);
        var vm = new AssessmentIndexViewModel
        {
            Assessments = list.Select(a => new AssessmentListItemViewModel
            {
                Id = a.Id,
                Title = a.Title,
                CreatedUtc = a.CreatedUtc,
                UpdatedUtc = a.UpdatedUtc
            }).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new AssessmentCreateInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentCreateInputModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _assessmentService.CreateAssessmentAsync(UserId(), model.Title, ct);
            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Manage(int id, CancellationToken ct)
    {
        var vm = await _assessmentService.GetManageModelAsync(id, UserId(), ct);
        if (vm == null)
            return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRow(int assessmentId, int requirementId,
        ManagePostModel model, CancellationToken ct)
    {
        try
        {
            await _assessmentService.UpdateEntryAsync(assessmentId, UserId(), requirementId,
                new DTOs.AssessmentEntryPatchDto
                {
                    Status = model.Status,
                    SourceCodeReference = model.SourceCodeReference,
                    Comment = model.Comment,
                    ToolUsed = model.ToolUsed
                }, ct);
            TempData["Flash"] = $"Saved requirement #{requirementId}.";
        }
        catch (Exception ex)
        {
            TempData["FlashError"] = ex.Message;
        }

        return RedirectToAction(nameof(Manage), new { id = assessmentId });
    }

    private string UserId() =>
        HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("User context missing.");
}

public class ManagePostModel
{
    public AssessmentStatus Status { get; set; }
    public string? SourceCodeReference { get; set; }
    public string? Comment { get; set; }
    public string? ToolUsed { get; set; }
}
