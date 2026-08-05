using GrokCareerCoach.Api.Data.Entities;

namespace GrokCareerCoach.Api.Services;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(AppUser user);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
