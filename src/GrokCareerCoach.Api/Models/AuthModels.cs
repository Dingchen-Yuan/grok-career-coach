using System.ComponentModel.DataAnnotations;

namespace GrokCareerCoach.Api.Models;

public sealed record GoogleLoginRequest(
    [Required] string IdToken);

public sealed record RefreshRequest(
    [Required] string RefreshToken);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserResponse User);

public sealed record UserResponse(Guid Id, string Email);
