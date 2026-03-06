using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Notifications.Commands.DeleteNotification;

public sealed class DeleteNotificationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteNotificationCommand>
{
    public async Task Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.ToUserId == userId && !n.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        notification.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
