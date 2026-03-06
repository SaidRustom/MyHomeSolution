using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    string Title,
    string? Description,
    string ToUserId,
    Guid? RelatedEntityId,
    string? RelatedEntityType) : INotification;
