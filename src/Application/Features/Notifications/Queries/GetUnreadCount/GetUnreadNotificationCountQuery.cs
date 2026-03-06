using MediatR;

namespace MyHomeSolution.Application.Features.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadNotificationCountQuery : IRequest<int>;
