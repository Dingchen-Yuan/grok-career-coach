using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Options;
using GrokCareerCoach.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GrokCareerCoach.Api.Tests;

public sealed class AuthServiceTests
{
    private static readonly JwtOptions JwtOptions = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Secret = "test-secret-that-is-at-least-32-characters",
        AccessTokenMinutes = 20,
        RefreshTokenDays = 14
    };

    [Fact]
    public async Task LoginWithGoogleAsync_CreatesAndReusesGoogleUser()
    {
        await using var dbContext = CreateDbContext();
        var validator = new StubGoogleTokenValidator(
            new GoogleIdentity("google-subject", "User@Example.com"));
        var service = CreateService(dbContext, validator);

        var first = await service.LoginWithGoogleAsync(
            new GoogleLoginRequest("valid-google-token"));
        var second = await service.LoginWithGoogleAsync(
            new GoogleLoginRequest("valid-google-token"));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Equal("user@example.com", first.User.Email);
        var user = await dbContext.Users.SingleAsync();
        Assert.Equal("google-subject", user.GoogleSubject);
        Assert.Equal(2, await dbContext.RefreshTokens.CountAsync());
        Assert.DoesNotContain(
            dbContext.RefreshTokens,
            token => token.TokenHash == first.RefreshToken);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_RejectsInvalidGoogleToken()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(
            dbContext,
            new StubGoogleTokenValidator(null));

        var result = await service.LoginWithGoogleAsync(
            new GoogleLoginRequest("invalid-google-token"));

        Assert.Null(result);
        Assert.Empty(dbContext.Users);
    }

    [Fact]
    public async Task RefreshAsync_RotatesRefreshToken()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(
            dbContext,
            new StubGoogleTokenValidator(
                new GoogleIdentity("google-subject", "user@example.com")));
        var login = await service.LoginWithGoogleAsync(
            new GoogleLoginRequest("valid-google-token"));

        var refreshed = await service.RefreshAsync(
            new RefreshRequest(login!.RefreshToken));
        var replay = await service.RefreshAsync(
            new RefreshRequest(login.RefreshToken));

        Assert.NotNull(refreshed);
        Assert.Null(replay);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(2, await dbContext.RefreshTokens.CountAsync());
        Assert.Equal(
            1,
            await dbContext.RefreshTokens.CountAsync(
                token => token.RevokedAt != null));
    }

    private static AuthService CreateService(
        AppDbContext dbContext,
        IGoogleTokenValidator validator)
    {
        var options = Microsoft.Extensions.Options.Options.Create(JwtOptions);
        return new AuthService(
            dbContext,
            validator,
            new JwtTokenService(options),
            options);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StubGoogleTokenValidator(GoogleIdentity? identity)
        : IGoogleTokenValidator
    {
        public Task<GoogleIdentity?> ValidateAsync(
            string idToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(identity);
    }
}
