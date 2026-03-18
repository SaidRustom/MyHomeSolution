using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingList;

public sealed record UpdateShoppingListCommand : IRequest, IRequireEditAccess
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public ShoppingListCategory Category { get; init; }
    public DateOnly? DueDate { get; init; }
    public Guid? DefaultBudgetId { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => Id;
}
