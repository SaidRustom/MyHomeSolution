using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record TaskCreatedEvent(Guid TaskId, string Title) : INotification;
