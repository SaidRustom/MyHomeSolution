using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.DeleteShoppingList;

public sealed class DeleteShoppingListCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<DeleteShoppingListCommand>
{
    public async Task Handle(DeleteShoppingListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .FirstOrDefaultAsync(sl => sl.Id == request.Id && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.Id);

        var sharedWithUserIds = await dbContext.EntityShares
            .Where(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == shoppingList.Id
                && !s.IsDeleted
                && s.SharedWithUserId != userId)
            .Select(s => s.SharedWithUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        shoppingList.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ShoppingListDeletedEvent(shoppingList.Id, shoppingList.Title, userId, sharedWithUserIds),
            cancellationToken);
    }
}
