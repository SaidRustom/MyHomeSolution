using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingLists;

public sealed record GetShoppingListsQuery : IRequest<PaginatedList<ShoppingListBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ShoppingListCategory? Category { get; init; }
    public bool? IsCompleted { get; init; }
    public string? SearchTerm { get; init; }
}
