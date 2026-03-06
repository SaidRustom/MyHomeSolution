using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record OccurrenceOverdueEvent(
    Guid OccurrenceId,
    Guid TaskId,
    string TaskTitle,
    string? AssignedToUserId) : INotification;
