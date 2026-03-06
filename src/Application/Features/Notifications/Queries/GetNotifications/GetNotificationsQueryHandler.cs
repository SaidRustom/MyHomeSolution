using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Notifications.Common;

namespace MyHomeSolution.Application.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationBriefDto>>
{
    public async Task<PaginatedList<NotificationBriefDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.ToUserId == userId && !n.IsDeleted);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        if (request.Type.HasValue)
            query = query.Where(n => n.Type == request.Type.Value);

        var projected = query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationBriefDto
            {
                Id = n.Id,
                Title = n.Title,
                Type = n.Type,
                FromUserId = n.FromUserId,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });

        var page = await PaginatedList<NotificationBriefDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);

        var fromUserIds = page.Items
            .Where(n => !string.IsNullOrWhiteSpace(n.FromUserId))
            .Select(n => n.FromUserId!)
            .Distinct();
        var nameMap = await identityService.GetUserFullNamesByIdsAsync(fromUserIds, cancellationToken);

        var enriched = page.Items.Select(n => n with
        {
            FromUserFullName = n.FromUserId is not null
                ? nameMap.GetValueOrDefault(n.FromUserId)
                : null
        }).ToList();

        return new PaginatedList<NotificationBriefDto>(enriched, page.TotalCount, request.PageNumber, request.PageSize);
    }
}
