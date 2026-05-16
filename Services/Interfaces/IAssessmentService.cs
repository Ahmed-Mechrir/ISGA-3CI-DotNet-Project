using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.ViewModels.Assessment;

namespace AsvsSecurityAuditor.Services.Interfaces;

public interface IAssessmentService
{
    Task<int> CreateAssessmentAsync(string userId, string title, CancellationToken ct = default);

    Task UpdateEntryAsync(int assessmentId, string userId, int requirementEntityId,
        AssessmentEntryPatchDto dto, CancellationToken ct = default);

    Task<ManageAssessmentPageViewModel?> GetManageModelAsync(int assessmentId, string userId,
        CancellationToken ct = default);
}
