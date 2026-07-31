using GrokCareerCoach.Api.Models;

namespace GrokCareerCoach.Api.Services;

public interface IGrokClient
{
    Task<CoachingResponse> AnalyzeAsync(
        CoachingRequest request,
        CancellationToken cancellationToken = default);
}
