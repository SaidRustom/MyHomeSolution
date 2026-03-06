using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ConnectionRequestSentEvent(
    Guid ConnectionId,
    string RequesterId,
    string AddresseeId) : INotification;
