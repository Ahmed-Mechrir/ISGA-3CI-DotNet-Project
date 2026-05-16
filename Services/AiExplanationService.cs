using System.Text;
using System.Text.Json;
using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AsvsSecurityAuditor.Services;

public class AiExplanationService : IAiExplanationService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiExplanationService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExplainResponseDto> ExplainRequirementAsync(
        AsvsRequirementEntity requirement,
        ExplainRequestDto dto,
        CancellationToken ct = default)
    {
        var apiKey = !string.IsNullOrWhiteSpace(dto.ApiKey) ? dto.ApiKey : _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Gemini API key is not configured. Set Gemini:ApiKey or pass apiKey in the payload.");

        var model = dto.Model ?? _configuration["Gemini:DefaultModel"] ?? "gemini-2.0-flash";

        var cwePart = string.IsNullOrWhiteSpace(requirement.Cwe)
            ? ""
            : "CWE: CWE-" + requirement.Cwe.Trim().TrimStart(':') + "\n";

        var techPhrase = string.IsNullOrWhiteSpace(dto.Technology)
            ? ""
            : string.Concat(" for ", dto.Technology);

        var sb = new StringBuilder();
        sb.AppendLine("You are a cybersecurity specialist for OWASP ASVS 4.0. Explain clearly in concise English:");
        sb.AppendLine();
        sb.AppendLine("Requirement ID: " + requirement.RequirementRef);
        sb.AppendLine("Chapter: " + requirement.Chapter);
        sb.AppendLine("Area / sub-topic: " + requirement.Area);
        sb.AppendLine("ASVS level: " + requirement.Level);
        if (!string.IsNullOrEmpty(cwePart))
            sb.Append(cwePart);
        if (!string.IsNullOrWhiteSpace(dto.Technology))
            sb.AppendLine("Project stack: " + dto.Technology.Trim());
        sb.AppendLine();
        sb.AppendLine("Requirement text:");
        sb.AppendLine(requirement.VerificationRequirement);
        sb.AppendLine();
        sb.AppendLine("Respond with numbered sections:");
        sb.AppendLine("1. What this means (2–3 sentences)");
        sb.AppendLine("2. Why it matters (risk if neglected)");
        sb.AppendLine("3. How to verify / implement — concrete steps" + techPhrase);
        sb.AppendLine("4. Recommended tooling and techniques" + techPhrase);
        sb.AppendLine();
        sb.AppendLine("Be succinct and technical.");
        var prompt = sb.ToString();

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.35, maxOutputTokens = 1024 }
        };

        var client = _httpClientFactory.CreateClient();
        var url =
            "https://generativelanguage.googleapis.com/v1beta/models/" +
            Uri.EscapeDataString(model) + ":generateContent?key=" + Uri.EscapeDataString(apiKey);

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini HTTP {(int)response.StatusCode}: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return new ExplainResponseDto
        {
            Explanation = text ?? "",
            Model = model,
            RequirementRef = requirement.RequirementRef
        };
    }
}
