using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Bills;

public sealed record BillBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public required string PaidByUserId { get; init; }
    public string? PaidByUserFullName { get; init; }
    public bool HasReceipt { get; init; }
    public int SplitCount { get; init; }
    public bool IsFullyPaid { get; init; }
    public bool HasLinkedTask { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
