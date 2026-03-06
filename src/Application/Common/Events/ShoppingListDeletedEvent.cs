using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ShoppingListDeletedEvent(
    Guid ShoppingListId,
    string Title,
    string DeletedByUserId,
    IReadOnlyList<string> SharedWithUserIds) : INotification;
