using MediatR;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GroupShoppingItems;

public sealed record GroupShoppingItemsQuery(Guid ShoppingListId) : IRequest<ShoppingItemGroupResult>;
