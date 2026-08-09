using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Data.Entities;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GrokCareerCoach.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CoachingController(
    IGrokClient grokClient,
    AppDbContext dbContext,
    IDistributedCache cache) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        JsonSerializerOptions.Web;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
    };

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

        var cacheKey = BuildCacheKey(userId, request);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        CoachingResponse response;
        if (cached is not null)
        {
            response = JsonSerializer.Deserialize<CoachingResponse>(
                cached,
                JsonOptions)!;
        }
        else
        {
            try
            {
                response = await grokClient.AnalyzeAsync(
                    request,
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Coaching analysis failed");
            }

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response, JsonOptions),
                CacheOptions,
                cancellationToken);
        }

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

    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var session = await dbContext.CoachingSessions
            .FirstOrDefaultAsync(
                item => item.Id == id && item.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        dbContext.CoachingSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw =
            User.FindFirstValue("sub")
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out userId);
    }

    private static string BuildCacheKey(Guid userId, CoachingRequest request)
    {
        var payload = $"{userId}|{request.JobDescription}|{request.ResumeHighlights}";
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return $"coaching:analyze:{hash}";
    }
}
