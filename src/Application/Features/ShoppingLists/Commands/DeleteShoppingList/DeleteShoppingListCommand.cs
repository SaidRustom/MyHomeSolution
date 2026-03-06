using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.DeleteShoppingList;

public sealed record DeleteShoppingListCommand(Guid Id) : IRequest, IRequireEditAccess
{
    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => Id;
}
