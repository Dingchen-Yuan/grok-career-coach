namespace GrokCareerCoach.Api.Services;

public interface IGoogleTokenValidator
{
    Task<GoogleIdentity?> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default);
}

public sealed record GoogleIdentity(string Subject, string Email);
