using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record OccurrenceSkippedEvent(Guid OccurrenceId, Guid TaskId) : INotification;
