using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record ShareRevokedEvent(
    string EntityType,
    Guid EntityId,
    string SharedWithUserId,
    string RevokedByUserId) : INotification;
