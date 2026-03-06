using MediatR;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBillFromReceipt;

public sealed record CreateBillFromReceiptCommand : IRequest<BillDetailDto>
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream Content { get; init; }
    public BillCategory Category { get; init; } = BillCategory.General;
    public List<BillSplitRequest>? Splits { get; init; }
}

public sealed record BillSplitRequest
{
    public required string UserId { get; init; }
    public decimal? Percentage { get; init; }
}
