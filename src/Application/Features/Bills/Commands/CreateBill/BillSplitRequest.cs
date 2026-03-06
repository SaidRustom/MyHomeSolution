using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBill;

public sealed record BillSplitRequest
{
    public required string UserId { get; init; }
    public decimal? Percentage { get; init; }
}
