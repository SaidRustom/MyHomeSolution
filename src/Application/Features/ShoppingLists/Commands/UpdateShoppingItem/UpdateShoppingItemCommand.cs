using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingItem;

public sealed record UpdateShoppingItemCommand : IRequest, IRequireEditAccess
{
    public Guid ShoppingListId { get; init; }
    public Guid ItemId { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; } = 1;
    public string? Unit { get; init; }
    public string? Notes { get; init; }
    public int SortOrder { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => ShoppingListId;
}
