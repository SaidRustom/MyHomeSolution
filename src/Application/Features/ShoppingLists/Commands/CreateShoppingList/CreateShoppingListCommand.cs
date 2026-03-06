using MediatR;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.CreateShoppingList;

public sealed record CreateShoppingListCommand : IRequest<Guid>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public ShoppingListCategory Category { get; init; }
    public DateOnly? DueDate { get; init; }
}
