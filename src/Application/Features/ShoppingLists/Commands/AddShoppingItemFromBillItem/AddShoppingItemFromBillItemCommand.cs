using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Features.ShoppingLists.Common;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;

public sealed record AddShoppingItemFromBillItemCommand : IRequest<ShoppingItemDto>, IRequireEditAccess
{
    public Guid ShoppingListId { get; init; }
    public Guid BillItemId { get; init; }
    public int? QuantityOverride { get; init; }
    public string? UnitOverride { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => ShoppingListId;
}
