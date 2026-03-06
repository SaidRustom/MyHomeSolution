using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record OccurrenceRescheduledEvent(
    Guid OccurrenceId,
    Guid TaskId,
    string TaskTitle,
    DateOnly PreviousDate,
    DateOnly NewDate,
    string? RescheduledByUserId) : INotification;
