using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Options;
using Microsoft.Extensions.Options;

namespace GrokCareerCoach.Api.Services;

public sealed class GrokClient(
    HttpClient httpClient,
    IOptions<GrokOptions> options,
    ILogger<GrokClient> logger) : IGrokClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly GrokOptions _options = options.Value;

    public async Task<CoachingResponse> AnalyzeAsync(
        CoachingRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            You are a concise career coach. Analyze fit between the job description and résumé highlights.
            Return ONLY valid JSON with this shape:
            {
              "fitSummary": "string",
              "strengths": ["string"],
              "gaps": ["string"],
              "interviewQuestions": ["string"],
              "improvementSuggestions": ["string"]
            }

            Job description:
            {{request.JobDescription}}

            Résumé highlights:
            {{request.ResumeHighlights}}
            """;

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "chat/completions");
        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        message.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = _options.Model,
                temperature = 0.2,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = "Return only JSON." },
                    new { role = "user", content = prompt }
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(
            message,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Grok API returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException(
                "Grok API rejected the request. Check GROK_API_KEY at https://console.x.ai.");
        }

        var completion = JsonSerializer.Deserialize<GrokChatCompletion>(
            body,
            JsonOptions)
            ?? throw new InvalidOperationException("Grok response was empty.");
        var content = completion.Choices.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException("Grok response had no content.");
        var coaching = JsonSerializer.Deserialize<CoachingResponse>(
            content,
            JsonOptions)
            ?? throw new InvalidOperationException(
                "Grok response JSON could not be parsed.");

        return coaching;
    }

    private sealed record GrokChatCompletion(
        [property: JsonPropertyName("choices")] IReadOnlyList<GrokChoice> Choices);

    private sealed record GrokChoice(
        [property: JsonPropertyName("message")] GrokMessage? Message);

    private sealed record GrokMessage(
        [property: JsonPropertyName("content")] string? Content);
}
