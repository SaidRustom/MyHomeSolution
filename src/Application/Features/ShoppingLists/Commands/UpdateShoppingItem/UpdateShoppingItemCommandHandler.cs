using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingItem;

public sealed class UpdateShoppingItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateShoppingItemCommand>
{
    public async Task Handle(UpdateShoppingItemCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var item = shoppingList.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException(nameof(ShoppingItem), request.ItemId);

        item.Name = request.Name;
        item.Quantity = request.Quantity;
        item.Unit = request.Unit;
        item.Notes = request.Notes;
        item.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
