using BlazorUI.Models.Bills;

namespace BlazorUI.Models.ShoppingLists;

public sealed record ProcessReceiptResultDto
{
    public Guid BillId { get; init; }
    public required BillDetailDto Bill { get; init; }
    public IReadOnlyList<ShoppingItemDto> CheckedItems { get; init; } = [];
    public IReadOnlyList<ShoppingItemDto> AddedItems { get; init; } = [];
    public IReadOnlyList<CrossListMatchDto> CrossListMatches { get; init; } = [];
}

public sealed record CrossListMatchDto
{
    public required string ReceiptItemName { get; init; }
    public required string GenericName { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public bool IsTaxable { get; init; }
    public IReadOnlyList<CrossListTargetDto> MatchingLists { get; init; } = [];
}

public sealed record CrossListTargetDto
{
    public Guid ShoppingListId { get; init; }
    public required string ShoppingListTitle { get; init; }
    public Guid ShoppingItemId { get; init; }
    public required string ShoppingItemName { get; init; }
}
