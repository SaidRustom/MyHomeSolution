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
    public string? AssignedToUserFullName { get; init; }
    public string? AssignedToUserAvatarUrl { get; init; }
    public string? CreatedByUserId { get; init; }
    public string? CreatedByUserFullName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool AutoCreateBill { get; init; }
    public decimal? DefaultBillAmount { get; init; }
    public string? DefaultBillCurrency { get; init; }
    public BillCategory? DefaultBillCategory { get; init; }
    public string? DefaultBillTitle { get; init; }
    public string? DefaultBillPaidByUserId { get; init; }
    public string? DefaultBillPaidByUserFullName { get; init; }
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
    public IReadOnlyCollection<RecurrenceAssigneeDto> Assignees { get; init; } = [];
}

public sealed record RecurrenceAssigneeDto
{
    public required string UserId { get; init; }
    public string? FullName { get; init; }
    public string? AvatarUrl { get; init; }
    public int Order { get; init; }
}

public sealed record OccurrenceDto
{
    public Guid Id { get; init; }
    public DateOnly DueDate { get; init; }
    public OccurrenceStatus Status { get; init; }
    public string? AssignedToUserId { get; init; }
    public string? AssignedToUserFullName { get; init; }
    public string? AssignedToUserAvatarUrl { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? CompletedByUserId { get; init; }
    public string? CompletedByUserFullName { get; init; }
    public string? CompletedByUserAvatarUrl { get; init; }
    public string? Notes { get; init; }
    public Guid? BillId { get; init; }
    public OccurrenceBillBriefDto? Bill { get; init; }
}

public sealed record OccurrenceBillBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public int TotalSplits { get; init; }
    public int PaidSplits { get; init; }
}
