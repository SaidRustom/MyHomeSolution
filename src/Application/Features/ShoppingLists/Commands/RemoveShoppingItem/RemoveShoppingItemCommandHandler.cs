using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.RemoveShoppingItem;

public sealed class RemoveShoppingItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RemoveShoppingItemCommand>
{
    public async Task Handle(RemoveShoppingItemCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var item = shoppingList.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException(nameof(ShoppingItem), request.ItemId);

        dbContext.ShoppingItems.Remove(item);
        shoppingList.Items.Remove(item);

        // Recompute completion state after removal
        if (shoppingList.Items.Count > 0)
        {
            var allChecked = shoppingList.Items.All(i => i.IsChecked);
            shoppingList.IsCompleted = allChecked;
            shoppingList.CompletedAt = allChecked ? dateTimeProvider.UtcNow : null;
        }
        else
        {
            shoppingList.IsCompleted = false;
            shoppingList.CompletedAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
