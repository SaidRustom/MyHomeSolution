using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingList;

public sealed class UpdateShoppingListCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<UpdateShoppingListCommand>
{
    public async Task Handle(UpdateShoppingListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = await dbContext.ShoppingLists
            .FirstOrDefaultAsync(sl => sl.Id == request.Id && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.Id);

        shoppingList.Title = request.Title;
        shoppingList.Description = request.Description;
        shoppingList.Category = request.Category;
        shoppingList.DueDate = request.DueDate;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ShoppingListUpdatedEvent(shoppingList.Id, shoppingList.Title, userId),
            cancellationToken);
    }
}
