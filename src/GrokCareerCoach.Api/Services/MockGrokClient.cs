using GrokCareerCoach.Api.Models;

namespace GrokCareerCoach.Api.Services;

public sealed class MockGrokClient : IGrokClient
{
    public Task<CoachingResponse> AnalyzeAsync(
        CoachingRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleHint = request.JobDescription.Split(
                ['\n', '.', '!'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "the target role";
        var highlightHint = request.ResumeHighlights.Split(
                ['\n', '.', '!'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "your highlighted experience";

        var response = new CoachingResponse(
            $"Based on \"{Truncate(roleHint, 90)}\", your highlight \"{Truncate(highlightHint, 90)}\" shows a promising baseline match. Connect each résumé claim to a measurable result before applying.",
            [
                "Relevant experience is visible in the submitted highlights.",
                "The profile can be tailored directly to the role."
            ],
            [
                "The current highlights do not yet prove impact with metrics.",
                "Role-specific tools and domain knowledge need clearer evidence."
            ],
            [
                "Which past project best demonstrates the core requirements of this role?",
                "Tell me about a measurable result you delivered under a tight deadline.",
                "What would your first 30 days in this position look like?"
            ],
            [
                "Add two quantified accomplishments to the résumé.",
                "Mirror the job description's key skills using truthful examples.",
                "Prepare a concise STAR story for each major requirement."
            ]);

        return Task.FromResult(response);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
