using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Occurrences;

public sealed record CalendarOccurrenceDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public required string TaskTitle { get; init; }
    public TaskPriority TaskPriority { get; init; }
    public TaskCategory TaskCategory { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public DateOnly DueDate { get; init; }
    public OccurrenceStatus Status { get; init; }
    public string? AssignedToUserId { get; init; }
    public string? AssignedToUserFullName { get; init; }
    public string? CompletedByUserId { get; init; }
    public string? CompletedByUserFullName { get; init; }
    public Guid? BillId { get; init; }
    public bool IsRecurring { get; init; }
    public bool HasBill { get; init; }
    public bool IsPrivate { get; init; }
}
