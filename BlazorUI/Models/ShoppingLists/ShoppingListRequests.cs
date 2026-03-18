using BlazorUI.Models.Enums;

namespace BlazorUI.Models.ShoppingLists;

public sealed record CreateShoppingListRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public ShoppingListCategory Category { get; init; }
    public DateOnly? DueDate { get; init; }
    public Guid? DefaultBudgetId { get; init; }
}

public sealed record UpdateShoppingListRequest
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public ShoppingListCategory Category { get; init; }
    public DateOnly? DueDate { get; init; }
    public Guid? DefaultBudgetId { get; init; }
}

public sealed record AddShoppingItemRequest
{
    public Guid ShoppingListId { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; } = 1;
    public string? Unit { get; init; }
    public string? Notes { get; init; }
}

public sealed record UpdateShoppingItemRequest
{
    public Guid ShoppingListId { get; init; }
    public Guid ItemId { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; } = 1;
    public string? Unit { get; init; }
    public string? Notes { get; init; }
    public int SortOrder { get; init; }
}

public sealed record AddShoppingItemFromBillItemRequest
{
    public Guid ShoppingListId { get; init; }
    public Guid BillItemId { get; init; }
    public int? QuantityOverride { get; init; }
    public string? UnitOverride { get; init; }
}

public sealed record ResolveCrossListMatchRequest
{
    public Guid TargetShoppingListId { get; init; }
    public Guid BillId { get; init; }
    public required string ReceiptItemName { get; init; }
    public required string GenericName { get; init; }
    public decimal Price { get; init; }
    public bool IsTaxable { get; init; }
    public bool ToggleExisting { get; init; } = true;
    public Guid? ShoppingItemId { get; init; }
}
