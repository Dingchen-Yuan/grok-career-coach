using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GrokCareerCoach.Api.Controllers;
using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Data.Entities;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

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
    public async Task Analyze_WithNameIdentifierClaim_PersistsCoachingSession()
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

    [Fact]
    public async Task Analyze_WithSubClaim_PersistsCoachingSession()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            "test");
        var controller = CreateController(
            dbContext,
            new ClaimsPrincipal(identity));

        var result = await controller.Analyze(Request, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<CoachingResponse>(ok.Value);
        var session = await dbContext.CoachingSessions.SingleAsync();
        Assert.Equal(userId, session.UserId);
    }

    [Fact]
    public async Task Analyze_UsesRedisCache_OnSecondCall()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim("sub", userId.ToString())],
            "test");
        var cache = CreateCache();
        var grok = new CountingGrokClient();
        var controller = new CoachingController(grok, dbContext, cache)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };

        await controller.Analyze(Request, default);
        await controller.Analyze(Request, default);

        Assert.Equal(1, grok.CallCount);
        Assert.Equal(2, await dbContext.CoachingSessions.CountAsync());
    }

    [Fact]
    public async Task DeleteSession_RemovesOwnSession()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = new CoachingSession
        {
            UserId = userId,
            JobDescription = Request.JobDescription,
            ResumeHighlights = Request.ResumeHighlights,
            ResponseJson = """{"fitSummary":"ok","strengths":[],"gaps":[],"interviewQuestions":[],"improvementSuggestions":[]}"""
        };
        dbContext.CoachingSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var identity = new ClaimsIdentity(
            [new Claim("sub", userId.ToString())],
            "test");
        var controller = CreateController(
            dbContext,
            new ClaimsPrincipal(identity));

        var result = await controller.DeleteSession(session.Id, default);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(dbContext.CoachingSessions);
    }

    [Fact]
    public async Task DeleteSession_WhenMissingOrOtherUser_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var session = new CoachingSession
        {
            UserId = ownerId,
            JobDescription = Request.JobDescription,
            ResumeHighlights = Request.ResumeHighlights,
            ResponseJson = """{"fitSummary":"ok","strengths":[],"gaps":[],"interviewQuestions":[],"improvementSuggestions":[]}"""
        };
        dbContext.CoachingSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var identity = new ClaimsIdentity(
            [new Claim("sub", otherId.ToString())],
            "test");
        var controller = CreateController(
            dbContext,
            new ClaimsPrincipal(identity));

        var otherUserResult = await controller.DeleteSession(session.Id, default);
        var missingResult = await controller.DeleteSession(Guid.NewGuid(), default);

        Assert.IsType<NotFoundResult>(otherUserResult);
        Assert.IsType<NotFoundResult>(missingResult);
        Assert.Single(dbContext.CoachingSessions);
    }

    private static CoachingController CreateController(
        AppDbContext dbContext,
        ClaimsPrincipal user) =>
        new(new MockGrokClient(), dbContext, CreateCache())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

    private static IDistributedCache CreateCache() =>
        new MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new MemoryDistributedCacheOptions()));

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class CountingGrokClient : IGrokClient
    {
        public int CallCount { get; private set; }

        public Task<CoachingResponse> AnalyzeAsync(
            CoachingRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new CoachingResponse(
                "summary",
                ["strength"],
                ["gap"],
                ["question"],
                ["suggestion"]));
        }
    }
}
