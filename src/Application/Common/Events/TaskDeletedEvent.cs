using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record TaskDeletedEvent(
    Guid TaskId,
    string Title,
    int DeletedOccurrenceCount,
    int DeletedBillCount,
    List<string> AffectedUserIds) : INotification;
