using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ToggleAllShoppingItems;

public sealed class ToggleAllShoppingItemsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ToggleAllShoppingItemsCommand>
{
    public async Task Handle(ToggleAllShoppingItemsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var now = dateTimeProvider.UtcNow;

        foreach (var item in shoppingList.Items)
        {
            if (item.IsChecked == request.Check)
                continue;

            item.IsChecked = request.Check;

            if (request.Check)
            {
                item.CheckedAt = now;
                item.CheckedByUserId = userId;
            }
            else
            {
                item.CheckedAt = null;
                item.CheckedByUserId = null;
            }
        }

        var allChecked = shoppingList.Items.All(i => i.IsChecked);
        shoppingList.IsCompleted = allChecked;
        shoppingList.CompletedAt = allChecked ? now : null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
