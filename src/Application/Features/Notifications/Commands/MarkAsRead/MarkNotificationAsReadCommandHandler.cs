using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Notifications.Commands.MarkAsRead;

public sealed class MarkNotificationAsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.ToUserId == userId && !n.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
