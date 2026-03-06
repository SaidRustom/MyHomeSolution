using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingListById;

public sealed class GetShoppingListByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetShoppingListByIdQuery, ShoppingListDetailDto>
{
    public async Task<ShoppingListDetailDto> Handle(
        GetShoppingListByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .AsNoTracking()
            .Include(sl => sl.Items.OrderBy(i => i.SortOrder))
            .Where(sl => !sl.IsDeleted)
            .FirstOrDefaultAsync(sl => sl.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.Id);

        var isOwner = shoppingList.CreatedBy == userId;
        var isShared = !isOwner && await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == shoppingList.Id
                && s.SharedWithUserId == userId
                && !s.IsDeleted, cancellationToken);

        if (!isOwner && !isShared)
            throw new ForbiddenAccessException();

        return new ShoppingListDetailDto
        {
            Id = shoppingList.Id,
            Title = shoppingList.Title,
            Description = shoppingList.Description,
            Category = shoppingList.Category,
            DueDate = shoppingList.DueDate,
            IsCompleted = shoppingList.IsCompleted,
            CompletedAt = shoppingList.CompletedAt,
            CreatedAt = shoppingList.CreatedAt,
            CreatedBy = shoppingList.CreatedBy,
            LastModifiedAt = shoppingList.LastModifiedAt,
            Items = shoppingList.Items.Select(i => new ShoppingItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Notes = i.Notes,
                IsChecked = i.IsChecked,
                CheckedAt = i.CheckedAt,
                CheckedByUserId = i.CheckedByUserId,
                SortOrder = i.SortOrder
            }).ToList()
        };
    }
}
