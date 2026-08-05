using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Data.Entities;
using GrokCareerCoach.Api.Models;
using GrokCareerCoach.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GrokCareerCoach.Api.Services;

public sealed class AuthService(
    AppDbContext dbContext,
    IGoogleTokenValidator googleTokenValidator,
    IJwtTokenService tokenService,
    IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<TokenResponse?> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = await googleTokenValidator.ValidateAsync(
            request.IdToken,
            cancellationToken);
        if (identity is null)
        {
            return null;
        }

        var email = NormalizeEmail(identity.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.GoogleSubject == identity.Subject,
            cancellationToken);
        if (user is null)
        {
            user = new AppUser
            {
                Email = email,
                GoogleSubject = identity.Subject
            };
            dbContext.Users.Add(user);
        }
        else if (user.Email != email)
        {
            user.Email = email;
        }

        var response = IssueTokens(user, out var refreshToken);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<TokenResponse?> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var existingToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
        {
            return null;
        }

        var response = IssueTokens(existingToken.User, out var replacement);
        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByTokenId = replacement.Id;
        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    private TokenResponse IssueTokens(
        AppUser user,
        out RefreshToken refreshToken)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var rawRefreshToken = tokenService.CreateRefreshToken();
        refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(rawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(
                _options.RefreshTokenDays)
        };

        return new TokenResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            rawRefreshToken,
            refreshToken.ExpiresAt,
            new UserResponse(user.Id, user.Email));
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
