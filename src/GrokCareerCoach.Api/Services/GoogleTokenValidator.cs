using Google.Apis.Auth;
using GrokCareerCoach.Api.Options;
using Microsoft.Extensions.Options;

namespace GrokCareerCoach.Api.Services;

public sealed class GoogleTokenValidator(IOptions<GoogleOptions> options)
    : IGoogleTokenValidator
{
    private readonly GoogleOptions _options = options.Value;

    public async Task<GoogleIdentity?> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            return null;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_options.ClientId]
                });
            cancellationToken.ThrowIfCancellationRequested();

            return payload.EmailVerified
                ? new GoogleIdentity(payload.Subject, payload.Email)
                : null;
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
