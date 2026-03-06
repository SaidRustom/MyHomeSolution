using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ShoppingListUpdatedEvent(
    Guid ShoppingListId,
    string Title,
    string UpdatedByUserId) : INotification;
