using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Dashboard.Common;

public sealed record RequiresAttentionDto
{
    public IReadOnlyList<AttentionBillDto> UnpaidBills { get; init; } = [];
    public IReadOnlyList<AttentionTaskDto> UrgentTasks { get; init; } = [];
}

public sealed record AttentionBillDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public string? PaidByUserFullName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record AttentionTaskDto
{
    public Guid TaskId { get; init; }
    public Guid OccurrenceId { get; init; }
    public required string Title { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public DateOnly DueDate { get; init; }
    public OccurrenceStatus Status { get; init; }
    public string? AssignedToUserFullName { get; init; }
}
