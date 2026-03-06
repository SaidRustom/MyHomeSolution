using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record BillUpdatedEvent(
    Guid BillId,
    string Title,
    string UpdatedByUserId) : INotification;
