using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Tasks;

public sealed record UpdateTaskRequest
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? AssignedToUserId { get; init; }
}
