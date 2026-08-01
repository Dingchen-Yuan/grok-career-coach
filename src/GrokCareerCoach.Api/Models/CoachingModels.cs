using System.ComponentModel.DataAnnotations;

namespace GrokCareerCoach.Api.Models;

public sealed record CoachingRequest(
    [Required, MinLength(30)] string JobDescription,
    [Required, MinLength(20)] string ResumeHighlights);

public sealed record CoachingResponse(
    string FitSummary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> InterviewQuestions,
    IReadOnlyList<string> ImprovementSuggestions);

public sealed record CoachingSessionResponse(
    Guid Id,
    string JobDescription,
    string ResumeHighlights,
    CoachingResponse Result,
    DateTimeOffset CreatedAt);
