using MediatR;

namespace MyHomeSolution.Application.Features.Notifications.Commands.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid Id) : IRequest;
