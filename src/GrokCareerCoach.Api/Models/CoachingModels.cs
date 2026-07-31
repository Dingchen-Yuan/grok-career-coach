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
