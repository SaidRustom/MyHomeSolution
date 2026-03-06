using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Notifications.Queries.GetNotificationById;

public sealed class GetNotificationByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetNotificationByIdQuery, NotificationDetailDto>
{
    public async Task<NotificationDetailDto> Handle(
        GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var notification = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => !n.IsDeleted && n.ToUserId == userId)
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        return new NotificationDetailDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Description = notification.Description,
            Type = notification.Type,
            FromUserId = notification.FromUserId,
            ToUserId = notification.ToUserId,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}
