using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.RemoveShoppingItem;

public sealed record RemoveShoppingItemCommand : IRequest, IRequireEditAccess
{
    public Guid ShoppingListId { get; init; }
    public Guid ItemId { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => ShoppingListId;
}
