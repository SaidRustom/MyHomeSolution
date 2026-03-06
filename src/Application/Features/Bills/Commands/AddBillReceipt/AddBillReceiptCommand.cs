using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.Bills.Commands.AddBillReceipt;

public sealed record AddBillReceiptCommand : IRequest<string>, IRequireEditAccess
{
    public Guid BillId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream Content { get; init; }

    public string ResourceType => EntityTypes.Bill;
    public Guid ResourceId => BillId;
}
