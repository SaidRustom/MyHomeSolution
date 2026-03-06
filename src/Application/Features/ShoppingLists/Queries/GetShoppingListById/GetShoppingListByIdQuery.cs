using MediatR;
using MyHomeSolution.Application.Features.ShoppingLists.Common;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingListById;

public sealed record GetShoppingListByIdQuery(Guid Id) : IRequest<ShoppingListDetailDto>;
