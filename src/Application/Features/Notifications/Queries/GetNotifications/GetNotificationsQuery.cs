using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Notifications.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery : IRequest<PaginatedList<NotificationBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsRead { get; init; }
    public NotificationType? Type { get; init; }
}
