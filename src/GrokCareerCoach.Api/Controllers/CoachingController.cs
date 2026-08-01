using System.Security.Claims;
using System.Text.Json;
using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Data.Entities;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrokCareerCoach.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CoachingController(
    IGrokClient grokClient,
    AppDbContext dbContext) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        JsonSerializerOptions.Web;

    [HttpPost("analyze")]
    [ProducesResponseType<CoachingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CoachingResponse>> Analyze(
        CoachingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await grokClient.AnalyzeAsync(request, cancellationToken);
        dbContext.CoachingSessions.Add(new CoachingSession
        {
            UserId = userId,
            JobDescription = request.JobDescription,
            ResumeHighlights = request.ResumeHighlights,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<CoachingSessionResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CoachingSessionResponse>>>
        GetSessions(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var sessions = await dbContext.CoachingSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        var response = sessions.Select(session => new CoachingSessionResponse(
            session.Id,
            session.JobDescription,
            session.ResumeHighlights,
            JsonSerializer.Deserialize<CoachingResponse>(
                session.ResponseJson,
                JsonOptions)!,
            session.CreatedAt)).ToList();

        return Ok(response);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
}
