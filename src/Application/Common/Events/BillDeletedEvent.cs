using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record BillDeletedEvent(
    Guid BillId,
    string Title,
    string DeletedByUserId,
    IReadOnlyList<string> AffectedUserIds) : INotification;
