using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.ShoppingLists.Common;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetShoppingListsQuery, PaginatedList<ShoppingListBriefDto>>
{
    public async Task<PaginatedList<ShoppingListBriefDto>> Handle(
        GetShoppingListsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var sharedListIds = dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.ShoppingList
                && s.SharedWithUserId == userId
                && !s.IsDeleted)
            .Select(s => s.EntityId);

        var query = dbContext.ShoppingLists
            .AsNoTracking()
            .Where(sl => !sl.IsDeleted)
            .Where(sl => sl.CreatedBy == userId || sharedListIds.Contains(sl.Id));

        if (request.Category.HasValue)
            query = query.Where(sl => sl.Category == request.Category.Value);

        if (request.IsCompleted.HasValue)
            query = query.Where(sl => sl.IsCompleted == request.IsCompleted.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(sl =>
                sl.Title.Contains(request.SearchTerm) ||
                (sl.Description != null && sl.Description.Contains(request.SearchTerm)));

        var projected = query
            .OrderByDescending(sl => sl.Id)
            .Select(sl => new ShoppingListBriefDto
            {
                Id = sl.Id,
                Title = sl.Title,
                Category = sl.Category,
                DueDate = sl.DueDate,
                IsCompleted = sl.IsCompleted,
                TotalItems = sl.Items.Count,
                CheckedItems = sl.Items.Count(i => i.IsChecked),
                CreatedAt = sl.CreatedAt
            });

        return await PaginatedList<ShoppingListBriefDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
