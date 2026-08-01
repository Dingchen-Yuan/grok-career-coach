using System.ComponentModel.DataAnnotations;

namespace GrokCareerCoach.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required, MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    [Range(5, 120)]
    public int AccessTokenMinutes { get; init; } = 20;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;
}
