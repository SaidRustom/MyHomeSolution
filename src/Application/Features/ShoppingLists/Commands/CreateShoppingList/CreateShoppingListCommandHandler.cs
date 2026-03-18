using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<CreateShoppingListCommand, Guid>
{
    public async Task<Guid> Handle(CreateShoppingListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var shoppingList = new ShoppingList
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            DueDate = request.DueDate,
            DefaultBudgetId = request.DefaultBudgetId
        };

        dbContext.ShoppingLists.Add(shoppingList);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ShoppingListCreatedEvent(shoppingList.Id, shoppingList.Title, userId),
            cancellationToken);

        return shoppingList.Id;
    }
}
