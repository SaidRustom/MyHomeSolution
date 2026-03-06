using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ShoppingListCreatedEvent(
    Guid ShoppingListId,
    string Title,
    string CreatedByUserId) : INotification;
