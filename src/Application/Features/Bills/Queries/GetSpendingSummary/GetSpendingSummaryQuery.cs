using MediatR;
using MyHomeSolution.Application.Features.Bills.Common;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetSpendingSummary;

public sealed record GetSpendingSummaryQuery : IRequest<SpendingSummaryDto>
{
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
}
