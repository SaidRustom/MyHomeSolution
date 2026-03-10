using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Occurrences;

public sealed record OccurrenceDto
{
    public Guid Id { get; init; }
    public DateOnly DueDate { get; init; }
    public OccurrenceStatus Status { get; init; }
    public string? AssignedToUserId { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? CompletedByUserId { get; init; }
    public string? Notes { get; init; }
    public Guid? BillId { get; init; }
}

public sealed record CompleteOccurrenceRequest
{
    public string? Notes { get; init; }
}

public sealed record SkipOccurrenceRequest
{
    public string? Notes { get; init; }
    public bool ZeroLinkedBillBalance { get; init; }
}

public sealed record StartOccurrenceRequest
{
    public string? Notes { get; init; }
}

public sealed record RescheduleOccurrenceRequest
{
    public DateOnly NewDueDate { get; init; }
    public string? Notes { get; init; }
}
