using MediatR;
using MyHomeSolution.Application.Features.Notifications.Common;

namespace MyHomeSolution.Application.Features.Notifications.Queries.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id) : IRequest<NotificationDetailDto>;
