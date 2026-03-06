using MediatR;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBillReceipt;

public sealed record GetBillReceiptQuery(Guid BillId) : IRequest<BillReceiptResult?>;

public sealed record BillReceiptResult(Stream Content, string ContentType, string FileName);
