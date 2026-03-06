using MediatR;

namespace MyHomeSolution.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed record MarkAllNotificationsAsReadCommand : IRequest<int>;
