using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Bills;

public sealed record BillDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public required string PaidByUserId { get; init; }
    public string? PaidByUserFullName { get; init; }
    public string? ReceiptUrl { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<BillSplitDto> Splits { get; init; } = [];
    public IReadOnlyList<BillItemDto> Items { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? LastModifiedAt { get; init; }
}
