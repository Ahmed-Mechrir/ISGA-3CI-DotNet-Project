using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Models.Entities;

namespace AsvsSecurityAuditor.Services.Interfaces;

public interface IAiExplanationService
{
    Task<ExplainResponseDto> ExplainRequirementAsync(
        AsvsRequirementEntity requirement,
        ExplainRequestDto dto,
        CancellationToken ct = default);
}
