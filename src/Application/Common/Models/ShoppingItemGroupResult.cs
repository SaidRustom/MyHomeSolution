namespace MyHomeSolution.Application.Common.Models;

public sealed record ShoppingItemGroupResult
{
    public IReadOnlyList<ShoppingItemGroup> Groups { get; init; } = [];
}

public sealed record ShoppingItemGroup
{
    public required string Category { get; init; }
    public required string Icon { get; init; }
    public int SortOrder { get; init; }
    public IReadOnlyList<string> ItemNames { get; init; } = [];
}
