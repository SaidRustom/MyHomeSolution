using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record OccurrenceCompletedEvent(
    Guid OccurrenceId,
    Guid TaskId,
    string? CompletedByUserId) : INotification;
