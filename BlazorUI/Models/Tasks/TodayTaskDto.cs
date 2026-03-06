using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Tasks;

public sealed record TodayTaskDto
{
    public Guid TaskId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool IsRecurring { get; init; }
    public string? AssignedToUserId { get; init; }
    public IReadOnlyCollection<OccurrenceDto> Occurrences { get; init; } = [];
}
