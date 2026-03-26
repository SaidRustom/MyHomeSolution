namespace BlazorUI.Models.Demo;

public sealed class DemoStatusDto
{
    public bool IsDemoUser { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
}
