using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record TaskUpdatedEvent(Guid TaskId, string Title) : INotification;
