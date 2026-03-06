using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ShoppingItemCheckedEvent(
    Guid ShoppingListId,
    string ShoppingListTitle,
    string ItemName,
    string CheckedByUserId) : INotification;
