using MediatR;

namespace MyHomeSolution.Application.Common.Events;

public sealed record BillSplitPaidEvent(
    Guid BillId,
    Guid SplitId,
    string BillTitle,
    string PaidByUserId,
    decimal Amount) : INotification;
