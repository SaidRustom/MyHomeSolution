using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Application.Features.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadNotificationCountQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public async Task<int> Handle(
        GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        return await dbContext.Notifications
            .CountAsync(n => n.ToUserId == userId && !n.IsRead && !n.IsDeleted, cancellationToken);
    }
}
