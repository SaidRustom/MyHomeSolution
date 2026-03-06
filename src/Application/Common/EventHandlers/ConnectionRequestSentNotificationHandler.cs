using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ConnectionRequestSentNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider,
    IIdentityService identityService)
    : INotificationHandler<ConnectionRequestSentEvent>
{
    public async Task Handle(ConnectionRequestSentEvent notification, CancellationToken cancellationToken)
    {
        var requesterName = await identityService.GetUserNameByIdAsync(notification.RequesterId, cancellationToken)
            ?? "Someone";

        var entity = new Notification
        {
            Title = $"New connection request from {requesterName}",
            Description = $"{requesterName} sent you a connection request.",
            Type = NotificationType.ConnectionRequestReceived,
            FromUserId = notification.RequesterId,
            ToUserId = notification.AddresseeId,
            RelatedEntityId = notification.ConnectionId,
            RelatedEntityType = "UserConnection"
        };

        dbContext.Notifications.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await realtimeService.SendUserNotificationAsync(
            entity.ToUserId,
            new UserPushNotification
            {
                EventType = nameof(NotificationCreatedEvent),
                NotificationId = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                RelatedEntityId = entity.RelatedEntityId,
                RelatedEntityType = entity.RelatedEntityType,
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
