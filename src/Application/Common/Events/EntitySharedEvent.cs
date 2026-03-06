using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record EntitySharedEvent(
    Guid ShareId,
    string EntityType,
    Guid EntityId,
    string SharedWithUserId,
    string SharedByUserId) : INotification;
