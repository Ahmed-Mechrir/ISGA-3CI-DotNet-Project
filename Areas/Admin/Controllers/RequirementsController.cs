using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Repositories.Interfaces;
using AsvsSecurityAuditor.Security;
using AsvsSecurityAuditor.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class RequirementsController : Controller
{
    private readonly IRequirementRepository _repository;

    public RequirementsController(IRequirementRepository repository) => _repository = repository;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _repository.GetAllOrderedAsync(ct);
        var vm = list.Select(r => new RequirementListViewModel
        {
            Id = r.Id,
            RequirementRef = r.RequirementRef,
            Chapter = r.Chapter,
            Area = r.Area,
            Level = r.Level,
            PreMarkedNotApplicable = r.PreMarkedNotApplicable
        }).ToList();
        return View(vm);
    }

    public IActionResult Create() => View(new RequirementEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequirementEditViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _repository.FindByRequirementRefAsync(model.RequirementRef, ct) != null)
        {
            ModelState.AddModelError(nameof(model.RequirementRef), "This requirement reference already exists.");
            return View(model);
        }

        await _repository.AddAsync(new AsvsRequirementEntity
        {
            RequirementRef = model.RequirementRef.Trim(),
            Chapter = model.Chapter.Trim(),
            Area = model.Area.Trim(),
            Level = model.Level,
            LevelRaw = model.LevelRaw ?? "",
            Cwe = model.Cwe ?? "",
            Nist = model.Nist ?? "",
            VerificationRequirement = model.VerificationRequirement.Trim(),
            PreMarkedNotApplicable = model.PreMarkedNotApplicable,
            CreatedUtc = DateTime.UtcNow
        }, ct);

        await _repository.SaveChangesAsync(ct);
        TempData["Flash"] = "Requirement created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound();

        var vm = new RequirementEditViewModel
        {
            Id = entity.Id,
            RequirementRef = entity.RequirementRef,
            Chapter = entity.Chapter,
            Area = entity.Area,
            Level = entity.Level,
            LevelRaw = entity.LevelRaw,
            Cwe = entity.Cwe,
            Nist = entity.Nist,
            VerificationRequirement = entity.VerificationRequirement,
            PreMarkedNotApplicable = entity.PreMarkedNotApplicable
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RequirementEditViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();

        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var duplicate = await _repository.FindByRequirementRefAsync(model.RequirementRef, ct);
        if (duplicate != null && duplicate.Id != id)
        {
            ModelState.AddModelError(nameof(model.RequirementRef), "Another row already uses this reference.");
            return View(model);
        }

        entity.RequirementRef = model.RequirementRef.Trim();
        entity.Chapter = model.Chapter.Trim();
        entity.Area = model.Area.Trim();
        entity.Level = model.Level;
        entity.LevelRaw = model.LevelRaw ?? "";
        entity.Cwe = model.Cwe ?? "";
        entity.Nist = model.Nist ?? "";
        entity.VerificationRequirement = model.VerificationRequirement.Trim();
        entity.PreMarkedNotApplicable = model.PreMarkedNotApplicable;
        entity.UpdatedUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(ct);

        TempData["Flash"] = "Requirement updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound();
        return View(new RequirementListViewModel
        {
            Id = entity.Id,
            RequirementRef = entity.RequirementRef,
            Chapter = entity.Chapter,
            Area = entity.Area,
            Level = entity.Level,
            PreMarkedNotApplicable = entity.PreMarkedNotApplicable
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteIfUnusedAsync(id, ct);
        TempData["Flash"] = deleted
            ? "Requirement deleted."
            : "Requirement could not be deleted (referenced by assessments).";
        return RedirectToAction(nameof(Index));
    }
}
