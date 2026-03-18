using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ResolveCrossListMatch;

public sealed record ResolveCrossListMatchCommand : IRequest, IRequireEditAccess
{
    public Guid TargetShoppingListId { get; init; }
    public Guid BillId { get; init; }
    public required string ReceiptItemName { get; init; }
    public required string GenericName { get; init; }
    public decimal Price { get; init; }
    public bool IsTaxable { get; init; }

    /// <summary>
    /// If true, toggle the existing matching item as checked.
    /// If false, add as a new item to the target shopping list.
    /// </summary>
    public bool ToggleExisting { get; init; } = true;

    /// <summary>
    /// The specific shopping item to toggle (when ToggleExisting is true).
    /// </summary>
    public Guid? ShoppingItemId { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => TargetShoppingListId;
}
