using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ToggleAllShoppingItems;

public sealed record ToggleAllShoppingItemsCommand : IRequest, IRequireEditAccess
{
    public Guid ShoppingListId { get; init; }
    public bool Check { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => ShoppingListId;
}
