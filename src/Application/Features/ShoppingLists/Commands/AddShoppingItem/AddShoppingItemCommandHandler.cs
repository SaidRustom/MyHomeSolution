using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItem;

public sealed class AddShoppingItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddShoppingItemCommand, ShoppingItemDto>
{
    public async Task<ShoppingItemDto> Handle(AddShoppingItemCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var nextSortOrder = shoppingList.Items.Count > 0
            ? shoppingList.Items.Max(i => i.SortOrder) + 1
            : 0;

        var item = new ShoppingItem
        {
            ShoppingListId = shoppingList.Id,
            Name = request.Name,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Notes = request.Notes,
            SortOrder = nextSortOrder
        };

        dbContext.ShoppingItems.Add(item);

        if (shoppingList.IsCompleted)
        {
            shoppingList.IsCompleted = false;
            shoppingList.CompletedAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShoppingItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Notes = item.Notes,
            IsChecked = item.IsChecked,
            CheckedAt = item.CheckedAt,
            CheckedByUserId = item.CheckedByUserId,
            SortOrder = item.SortOrder
        };
    }
}
