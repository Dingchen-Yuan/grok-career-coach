using GrokCareerCoach.Api.Models;

namespace GrokCareerCoach.Api.Services;

public sealed class MockGrokClient : IGrokClient
{
    public Task<CoachingResponse> AnalyzeAsync(
        CoachingRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new CoachingResponse(
            "Your experience shows a promising baseline match. Connect each résumé claim to a measurable result before applying.",
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
}
