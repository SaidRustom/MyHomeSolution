using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Bills;

public sealed record CreateBillRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "$";
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public string? Notes { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? BudgetId { get; init; }
    public List<BillSplitRequest> Splits { get; init; } = [];
}

public sealed record BillSplitRequest
{
    public required string UserId { get; init; }
    public decimal? Percentage { get; init; }
}
