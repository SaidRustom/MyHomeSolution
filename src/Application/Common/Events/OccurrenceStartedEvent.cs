using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record OccurrenceStartedEvent(
    Guid OccurrenceId,
    Guid TaskId,
    string TaskTitle,
    string? StartedByUserId) : INotification;
