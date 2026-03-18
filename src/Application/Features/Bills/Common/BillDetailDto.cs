using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Common;

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
    public string? PaidByUserAvatarUrl { get; init; }
    public string? ReceiptUrl { get; init; }

    // Legacy single-relation fields (kept for backward compat)
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public string? RelatedEntityName { get; init; }

    public string? Notes { get; init; }
    public IReadOnlyList<BillRelatedItemDto> RelatedItems { get; init; } = [];
    public IReadOnlyList<BillSplitDto> Splits { get; init; } = [];
    public IReadOnlyList<BillItemDto> Items { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedByUserId { get; init; }
    public string? CreatedBy { get; init; }
    public string? CreatedByAvatarUrl { get; init; }
    public DateTimeOffset? LastModifiedAt { get; init; }
}

public sealed record BillRelatedItemDto
{
    public Guid Id { get; init; }
    public Guid RelatedEntityId { get; init; }
    public required string RelatedEntityType { get; init; }
    public string? RelatedEntityName { get; init; }
}
