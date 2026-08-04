using System.ComponentModel.DataAnnotations;

namespace GrokCareerCoach.Api.Options;

public sealed class GrokOptions
{
    public const string SectionName = "Grok";

    [Required]
    public string BaseUrl { get; set; } = "https://api.x.ai/v1";

    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = "grok-4";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
