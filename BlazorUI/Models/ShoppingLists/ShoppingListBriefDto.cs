using BlazorUI.Models.Enums;

namespace BlazorUI.Models.ShoppingLists;

public sealed record ShoppingListBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public ShoppingListCategory Category { get; init; }
    public DateOnly? DueDate { get; init; }
    public bool IsCompleted { get; init; }
    public int TotalItems { get; init; }
    public int CheckedItems { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
