using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record BillReceiptAddedEvent(
    Guid BillId,
    string BillTitle,
    string AddedByUserId) : INotification;
