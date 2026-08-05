using GrokCareerCoach.Api.Models;

namespace GrokCareerCoach.Api.Services;

public interface IAuthService
{
    Task<TokenResponse?> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenResponse?> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default);
}
