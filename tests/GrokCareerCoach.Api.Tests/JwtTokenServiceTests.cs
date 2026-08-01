using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GrokCareerCoach.Api.Data.Entities;
using GrokCareerCoach.Api.Options;
using GrokCareerCoach.Api.Services;
using Microsoft.Extensions.Options;

namespace GrokCareerCoach.Api.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_IncludesUserClaims()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Secret = "test-secret-that-is-at-least-32-characters"
        });
        var service = new JwtTokenService(options);
        var user = new AppUser
        {
            Email = "user@example.com",
            GoogleSubject = "google-subject"
        };

        var result = service.CreateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.NameIdentifier
                && claim.Value == user.Id.ToString());
        Assert.Contains(
            token.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Email
                && claim.Value == user.Email);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RefreshTokens_AreRandomAndHashDeterministically()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Secret = "test-secret-that-is-at-least-32-characters"
        });
        var service = new JwtTokenService(options);

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(
            service.HashRefreshToken(first),
            service.HashRefreshToken(first));
        Assert.NotEqual(first, service.HashRefreshToken(first));
    }
}
