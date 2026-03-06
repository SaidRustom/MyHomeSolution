using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ConnectionRequestAcceptedEvent(
    Guid ConnectionId,
    string RequesterId,
    string AcceptedByUserId) : INotification;
