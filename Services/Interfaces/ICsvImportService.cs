using AsvsSecurityAuditor.DTOs;

namespace AsvsSecurityAuditor.Services.Interfaces;

public interface ICsvImportService
{
    Task<ImportResultDto> ImportFromStreamAsync(Stream stream, CancellationToken ct = default);
}
