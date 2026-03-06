namespace MyHomeSolution.Application.Features.ShoppingLists.Common;

public sealed record ShoppingItemDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; }
    public string? Unit { get; init; }
    public string? Notes { get; init; }
    public bool IsChecked { get; init; }
    public DateTimeOffset? CheckedAt { get; init; }
    public string? CheckedByUserId { get; init; }
    public int SortOrder { get; init; }
}
