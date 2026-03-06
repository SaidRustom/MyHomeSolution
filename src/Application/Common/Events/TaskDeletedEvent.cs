using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record TaskDeletedEvent(Guid TaskId) : INotification;
