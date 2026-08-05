namespace GrokCareerCoach.Api.Data.Entities;

public sealed class CoachingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string JobDescription { get; set; }
    public required string ResumeHighlights { get; set; }
    public required string ResponseJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public AppUser User { get; set; } = null!;
}
