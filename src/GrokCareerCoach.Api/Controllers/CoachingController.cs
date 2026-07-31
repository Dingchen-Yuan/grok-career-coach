using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrokCareerCoach.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CoachingController(IGrokClient grokClient) : ControllerBase
{
    [HttpPost("analyze")]
    [ProducesResponseType<CoachingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CoachingResponse>> Analyze(
        CoachingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await grokClient.AnalyzeAsync(request, cancellationToken);
        return Ok(response);
    }
}
