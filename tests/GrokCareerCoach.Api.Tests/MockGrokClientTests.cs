using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;

namespace GrokCareerCoach.Api.Tests;

public sealed class MockGrokClientTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsStructuredCoachingResponse()
    {
        var client = new MockGrokClient();
        var request = new CoachingRequest(
            "We need a software engineer who can build reliable web APIs.",
            "Built and maintained several customer-facing APIs in C#.");

        var result = await client.AnalyzeAsync(request);

        Assert.NotEmpty(result.FitSummary);
        Assert.NotEmpty(result.Strengths);
        Assert.NotEmpty(result.Gaps);
        Assert.NotEmpty(result.InterviewQuestions);
        Assert.NotEmpty(result.ImprovementSuggestions);
    }
}
