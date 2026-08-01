using System.Security.Claims;
using GrokCareerCoach.Api.Controllers;
using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrokCareerCoach.Api.Tests;

public sealed class CoachingControllerTests
{
    private static readonly CoachingRequest Request = new(
        "We need an engineer who can build reliable web APIs and services.",
        "Built and maintained customer-facing APIs using C# and PostgreSQL.");

    [Fact]
    public async Task Analyze_WithoutUserClaim_ReturnsUnauthorized()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new ClaimsPrincipal());

        var result = await controller.Analyze(Request, default);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Empty(dbContext.CoachingSessions);
    }

    [Fact]
    public async Task Analyze_WithUserClaim_PersistsCoachingSession()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test");
        var controller = CreateController(
            dbContext,
            new ClaimsPrincipal(identity));

        var result = await controller.Analyze(Request, default);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<CoachingResponse>(response.Value);
        var session = await dbContext.CoachingSessions.SingleAsync();
        Assert.Equal(userId, session.UserId);
        Assert.Equal(Request.JobDescription, session.JobDescription);
        Assert.Contains("fitSummary", session.ResponseJson);
    }

    private static CoachingController CreateController(
        AppDbContext dbContext,
        ClaimsPrincipal user) =>
        new(new MockGrokClient(), dbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
