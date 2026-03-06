using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ToggleShoppingItem;

public sealed class ToggleShoppingItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<ToggleShoppingItemCommand>
{
    public async Task Handle(ToggleShoppingItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var item = shoppingList.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException(nameof(ShoppingItem), request.ItemId);

        item.IsChecked = !item.IsChecked;

        if (item.IsChecked)
        {
            item.CheckedAt = dateTimeProvider.UtcNow;
            item.CheckedByUserId = userId;
        }
        else
        {
            item.CheckedAt = null;
            item.CheckedByUserId = null;
        }

        var allChecked = shoppingList.Items.All(i => i.IsChecked);
        shoppingList.IsCompleted = allChecked;
        shoppingList.CompletedAt = allChecked ? dateTimeProvider.UtcNow : null;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (item.IsChecked)
        {
            await publisher.Publish(
                new ShoppingItemCheckedEvent(shoppingList.Id, shoppingList.Title, item.Name, userId),
                cancellationToken);
        }
    }
}
