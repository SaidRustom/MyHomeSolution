using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Notifications.Commands.CreateNotification;

public sealed class CreateNotificationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<CreateNotificationCommand, Guid>
{
    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var fromUserId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var notification = new Notification
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            FromUserId = fromUserId,
            ToUserId = request.ToUserId,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType,
            IsRead = false
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new NotificationCreatedEvent(
                notification.Id,
                notification.Title,
                notification.Description,
                notification.ToUserId,
                notification.RelatedEntityId,
                notification.RelatedEntityType),
            cancellationToken);

        return notification.Id;
    }
}
