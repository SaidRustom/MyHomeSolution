using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Tasks;

public sealed record CreateTaskRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool IsRecurring { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? AssignedToUserId { get; init; }
    public RecurrenceType? RecurrenceType { get; init; }
    public int? Interval { get; init; }
    public DateOnly? RecurrenceStartDate { get; init; }
    public DateOnly? RecurrenceEndDate { get; init; }
    public List<string>? AssigneeUserIds { get; init; }
    public bool AutoCreateBill { get; init; }
    public decimal? DefaultBillAmount { get; init; }
    public string? DefaultBillCurrency { get; init; }
    public BillCategory? DefaultBillCategory { get; init; }
    public string? DefaultBillTitle { get; init; }
    public string? DefaultBillPaidByUserId { get; init; }
    public Guid? DefaultBudgetId { get; init; }
}
