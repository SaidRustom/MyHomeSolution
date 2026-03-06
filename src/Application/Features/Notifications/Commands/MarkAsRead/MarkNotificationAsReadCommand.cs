using MediatR;

namespace MyHomeSolution.Application.Features.Notifications.Commands.MarkAsRead;

public sealed record MarkNotificationAsReadCommand(Guid Id) : IRequest;
