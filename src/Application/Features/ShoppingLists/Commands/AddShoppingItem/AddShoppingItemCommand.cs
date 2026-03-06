using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Features.ShoppingLists.Common;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItem;

public sealed record AddShoppingItemCommand : IRequest<ShoppingItemDto>, IRequireEditAccess
{
    public Guid ShoppingListId { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; } = 1;
    public string? Unit { get; init; }
    public string? Notes { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => ShoppingListId;
}
