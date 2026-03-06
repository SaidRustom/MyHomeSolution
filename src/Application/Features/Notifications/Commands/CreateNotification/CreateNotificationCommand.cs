using MediatR;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Notifications.Commands.CreateNotification;

public sealed record CreateNotificationCommand : IRequest<Guid>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public NotificationType Type { get; init; }
    public required string ToUserId { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
}
