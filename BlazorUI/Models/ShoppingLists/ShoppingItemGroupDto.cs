namespace BlazorUI.Models.ShoppingLists;

public sealed record ShoppingItemGroupResultDto
{
    public IReadOnlyList<ShoppingItemGroupDto> Groups { get; init; } = [];
}

public sealed record ShoppingItemGroupDto
{
    public required string Category { get; init; }
    public required string Icon { get; init; }
    public int SortOrder { get; init; }
    public IReadOnlyList<string> ItemNames { get; init; } = [];
}
