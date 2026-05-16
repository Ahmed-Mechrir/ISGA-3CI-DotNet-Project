using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Repositories.Interfaces;
using AsvsSecurityAuditor.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Controllers;

/// <summary>JSON helpers for SPA-style charts.</summary>
[Route("api")]
[ApiController]
[Authorize(Policy = "Auditor")]
public class MetricsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public MetricsApiController(IReportService reportService) => _reportService = reportService;

    [HttpGet("dashboard/stats/{assessmentId:int}")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(int assessmentId, CancellationToken ct)
    {
        var uid = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid))
            return Unauthorized();

        try
        {
            return Ok(await _reportService.BuildDashboardStatsAsync(assessmentId, uid, ct));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}

[Route("api/requirements")]
[ApiController]
[Authorize(Policy = "Auditor")]
public class RequirementsApiController : ControllerBase
{
    private readonly IAiExplanationService _ai;
    private readonly IRequirementRepository _repository;

    public RequirementsApiController(IAiExplanationService ai, IRequirementRepository repository)
    {
        _ai = ai;
        _repository = repository;
    }

    [HttpPost("{id:int}/explain")]
    public async Task<ActionResult<ExplainResponseDto>> Explain(int id, [FromBody] ExplainRequestDto body, CancellationToken ct)
    {
        var requirement = await _repository.GetByIdAsync(id, ct);
        if (requirement == null)
            return NotFound();

        try
        {
            var result = await _ai.ExplainRequirementAsync(requirement, body, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
