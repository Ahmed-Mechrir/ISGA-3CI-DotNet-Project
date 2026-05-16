using System.Globalization;
using System.IO;
using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Services;

public class CsvImportService : ICsvImportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(ApplicationDbContext db, ILogger<CsvImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ImportResultDto> ImportFromStreamAsync(Stream stream, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var inserted = 0;
        var updated = 0;

        using var sr = new StreamReader(stream, leaveOpen: true);
        var allText = await sr.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(allText))
        {
            errors.Add("CSV file is empty.");
            return new ImportResultDto { Errors = errors };
        }

        var firstLine = allText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var delimiter = firstLine.Count(c => c == ';') > firstLine.Count(c => c == ',') ? ";" : ",";

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = false
        };

        using var stringReader = new StringReader(allText);
        using var csv = new CsvReader(stringReader, config);

        if (!await csv.ReadAsync())
        {
            errors.Add("Could not read CSV.");
            return new ImportResultDto { Errors = errors };
        }

        csv.ReadHeader();
        var headerRecord = csv.HeaderRecord;
        if (headerRecord == null || headerRecord.Length == 0)
        {
            errors.Add("No header row detected.");
            return new ImportResultDto { Errors = errors };
        }

        var col = BuildHeaderMap(headerRecord);

        var ixRef = Idx(col, "#", "ID", "RequirementId", "Req", "Number", "Item", "Section");
        var ixChapter = Idx(col, "Category", "Chapter", "Chapter Name", "Group");
        var ixArea = Idx(col, "Area", "Sub-category", "Subcategory", "Sub Category");
        var ixLevel = Idx(col, "ASVS Level", "ASVSLevel", "Level");
        var ixCwe = Idx(col, "CWE");
        var ixNist = Idx(col, "NIST");
        var ixVerified = Idx(col, "Verification Requirement", "VerificationRequirement", "Requirement", "Verification", "Description", "Requirement Text");
        var ixValid = Idx(col, "Valid", "Status");

        if (ixRef == null || ixVerified == null)
        {
            errors.Add("CSV must contain a requirement reference column (# or ID or RequirementId) and a verification/requirement column.");
            return new ImportResultDto { Errors = errors };
        }

        var refsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowNum = 1;

        while (await csv.ReadAsync())
        {
            rowNum++;
            try
            {
                static string Cell(CsvReader r, int? index)
                {
                    if (index == null || index < 0)
                        return "";
                    try
                    {
                        return r.GetField(index.Value)?.Trim() ?? "";
                    }
                    catch
                    {
                        return "";
                    }
                }

                var reqRefRaw = NormalizeRef(Cell(csv, ixRef));
                var text = Cell(csv, ixVerified);
                if (string.IsNullOrWhiteSpace(reqRefRaw))
                    continue;

                if (!refsInFile.Add(reqRefRaw))
                {
                    errors.Add($"Row {rowNum}: duplicate RequirementRef '{reqRefRaw}' skipped.");
                    continue;
                }

                var chapter = ixChapter.HasValue ? Cell(csv, ixChapter) : "Uncategorized";
                if (string.IsNullOrWhiteSpace(chapter))
                    chapter = "Uncategorized";

                var area = ixArea.HasValue ? Cell(csv, ixArea) : "";
                var levelRaw = ixLevel.HasValue ? Cell(csv, ixLevel) : "";
                var cweRaw = ixCwe.HasValue ? Cell(csv, ixCwe) : "";
                var nist = ixNist.HasValue ? Cell(csv, ixNist) : "";
                var validCol = ixValid.HasValue ? Cell(csv, ixValid) : "";

                var preNa = validCol.Equals("Not Applicable", StringComparison.OrdinalIgnoreCase)
                           || validCol.Equals("N/A", StringComparison.OrdinalIgnoreCase);

                var level = ParseLevel(levelRaw);
                var cwe = cweRaw.Trim();

                var existing = await _db.Requirements
                    .FirstOrDefaultAsync(r => r.RequirementRef == reqRefRaw, ct);

                if (existing == null)
                {
                    await _db.Requirements.AddAsync(new AsvsRequirementEntity
                    {
                        RequirementRef = reqRefRaw,
                        Chapter = chapter,
                        Area = area,
                        Level = level,
                        LevelRaw = levelRaw,
                        Cwe = cwe,
                        Nist = nist,
                        VerificationRequirement = text,
                        PreMarkedNotApplicable = preNa
                    }, ct);
                    inserted++;
                }
                else
                {
                    existing.Chapter = chapter;
                    existing.Area = area;
                    existing.Level = level;
                    existing.LevelRaw = levelRaw;
                    existing.Cwe = cwe;
                    existing.Nist = nist;
                    existing.VerificationRequirement = text;
                    existing.PreMarkedNotApplicable = preNa;
                    existing.UpdatedUtc = DateTime.UtcNow;
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSV row {Row} failed.", rowNum);
                errors.Add($"Row {rowNum}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CSV import: inserted={Ins}, updated={Upd}", inserted, updated);

        return new ImportResultDto { Inserted = inserted, Updated = updated, Errors = errors };
    }

    private static Dictionary<string, int> BuildHeaderMap(string[] headerRecord)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerRecord.Length; i++)
        {
            var key = NormalizeHeader(headerRecord[i]);
            if (string.IsNullOrEmpty(key) || map.ContainsKey(key))
                continue;
            map[key] = i;
        }

        return map;
    }

    private static string NormalizeHeader(string? h) =>
        (h ?? "").Trim().Replace("\uFEFF", "").ToLowerInvariant();

    private static int? Idx(Dictionary<string, int> map, params string[] names)
    {
        foreach (var n in names)
        {
            var key = NormalizeHeader(n);
            if (map.TryGetValue(key, out var idx))
                return idx;
        }

        return null;
    }

    private static string NormalizeRef(string raw)
    {
        var s = raw.Trim();
        return s.StartsWith('V') && s.Length > 1 && char.IsDigit(s[1])
            ? s.ToUpperInvariant()
            : s;
    }

    private static int ParseLevel(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        if (int.TryParse(raw.Trim(), out var n)) return n;
        var clean = raw.ToUpperInvariant();
        if (clean.Contains("LVL3", StringComparison.Ordinal) || clean.Contains("LEVEL 3", StringComparison.Ordinal)) return 3;
        if (clean.Contains("LVL2", StringComparison.Ordinal) || clean.Contains("LEVEL 2", StringComparison.Ordinal)) return 2;
        if (clean.Contains("LVL1", StringComparison.Ordinal) || clean.Contains("LEVEL 1", StringComparison.Ordinal)) return 1;
        if (clean.Contains('3')) return 3;
        if (clean.Contains('2')) return 2;
        if (clean.Contains('1')) return 1;
        return 0;
    }
}
