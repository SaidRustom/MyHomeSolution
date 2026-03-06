using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record BillCreatedEvent(
    Guid BillId,
    string Title,
    decimal Amount,
    string PaidByUserId) : INotification;
