using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Tasks;

public sealed record TaskDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool IsRecurring { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? AssignedToUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public RecurrencePatternDto? RecurrencePattern { get; init; }
    public IReadOnlyCollection<OccurrenceDto> Occurrences { get; init; } = [];
}

public sealed record RecurrencePatternDto
{
    public Guid Id { get; init; }
    public RecurrenceType Type { get; init; }
    public int Interval { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public IReadOnlyCollection<string> AssigneeUserIds { get; init; } = [];
}

public sealed record OccurrenceDto
{
    public Guid Id { get; init; }
    public DateOnly DueDate { get; init; }
    public OccurrenceStatus Status { get; init; }
    public string? AssignedToUserId { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? Notes { get; init; }
}
