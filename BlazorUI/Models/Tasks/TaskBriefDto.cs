using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Tasks;

public sealed record TaskBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public bool IsRecurring { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? NextDueDate { get; init; }
    public string? AssignedToUserId { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool AutoCreateBill { get; init; }
}
