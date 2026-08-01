using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrokCareerCoach.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("google")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> GoogleLogin(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginWithGoogleAsync(
            request,
            cancellationToken);

        return response is null
            ? Unauthorized(new ProblemDetails
            {
                Title = "Google sign-in failed",
                Detail = "The Google ID token is invalid or expired.",
                Status = StatusCodes.Status401Unauthorized
            })
            : Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(
            request,
            cancellationToken);
        return response is null
            ? Unauthorized(new ProblemDetails
            {
                Title = "Token refresh failed",
                Detail = "The refresh token is invalid or expired.",
                Status = StatusCodes.Status401Unauthorized
            })
            : Ok(response);
    }
}
